using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SajhaSikshya.Data.Entities.Marketplace;
using SajhaSikshya.Data.Entities.Orders;
using SajhaSikshya.Data.Enums;
using SajhaSikshya.Data.ValueObjects;
using SajhaSikshya.DTOs.Notifications;
using SajhaSikshya.Repositories;
using SajhaSikshya.Services;
using SajhaSikshya.Services.Interfaces.Notifications;
using SajhaSikshya.Services.Orders;

namespace SajhaSikshya.Tests;

/// <summary>
/// Covers the order state machine end to end against a real (in-memory) EF Core
/// context — the exact behavior verified live, by hand, during the August 2026
/// end-to-end audit: reservation on order creation, stock only ever debited on
/// completion, and listings correctly released back to Active on cancellation.
/// </summary>
public class OrderServiceTests
{
    private static (OrderService service, SajhaSikshya.Data.ApplicationDbContext db) CreateService()
    {
        var db = TestDbContextFactory.Create();
        var notifications = new Mock<INotificationService>();
        notifications
            .Setup(n => n.CreateAsync(
                It.IsAny<string>(),
                It.IsAny<NotificationType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()))
            .ReturnsAsync(ServiceResult<int>.Success(1));

        var service = new OrderService(new UnitOfWork(db), notifications.Object, NullLogger<OrderService>.Instance);
        return (service, db);
    }

    private static Listing ActiveListing(int stock = 1, string sellerId = "seller-1") => new()
    {
        Title = "Test Listing For Order Machine",
        Slug = "test-listing-for-order-machine",
        Description = "A listing used purely to exercise the order state machine in tests.",
        Price = new Money(100m),
        Condition = BookCondition.Good,
        Status = ListingStatus.Active,
        StockQuantity = stock,
        SellerId = sellerId,
        CategoryId = 1,
        SubjectId = 1,
        AcademicLevelId = 1,
    };

    [Fact]
    public async Task CreateOrderAsync_ReservesTheListing_AndCreatesAPendingOrderWithHistory()
    {
        var (service, db) = CreateService();
        var listing = ActiveListing();
        db.Listings.Add(listing);
        await db.SaveChangesAsync();

        var result = await service.CreateOrderAsync("buyer-1", listing.Id);

        Assert.True(result.Succeeded);
        var reloadedListing = await db.Listings.FindAsync(listing.Id);
        Assert.Equal(ListingStatus.Reserved, reloadedListing!.Status);
        Assert.Equal(1, reloadedListing.StockQuantity); // stock is untouched at order time

        var order = await db.Orders.FindAsync(result.Data);
        Assert.NotNull(order);
        Assert.Equal(OrderStatus.Pending, order!.Status);
        Assert.StartsWith("ORD-", order.ReferenceNumber);

        var history = db.Set<OrderStatusHistory>().Where(h => h.OrderId == order.Id).ToList();
        var single = Assert.Single(history);
        Assert.Null(single.OldStatus);
        Assert.Equal(OrderStatus.Pending, single.NewStatus);
    }

    [Fact]
    public async Task CreateOrderAsync_RejectsWhenListingIsNotActive()
    {
        var (service, db) = CreateService();
        var listing = ActiveListing();
        listing.Status = ListingStatus.Reserved; // already has an order in flight
        db.Listings.Add(listing);
        await db.SaveChangesAsync();

        var result = await service.CreateOrderAsync("buyer-1", listing.Id);

        Assert.False(result.Succeeded);
        Assert.Empty(db.Orders);
    }

    [Fact]
    public async Task CreateOrderAsync_RejectsSellerOrderingTheirOwnListing()
    {
        var (service, db) = CreateService();
        var listing = ActiveListing(sellerId: "same-user");
        db.Listings.Add(listing);
        await db.SaveChangesAsync();

        var result = await service.CreateOrderAsync("same-user", listing.Id);

        Assert.False(result.Succeeded);
        Assert.Contains("own listing", result.Errors.First(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConfirmPickupAsync_DecrementsStockByOne_AndReturnsListingToActive_WhenStockRemains()
    {
        var (service, db) = CreateService();
        var listing = ActiveListing(stock: 2);
        listing.Status = ListingStatus.Reserved;
        db.Listings.Add(listing);
        var order = new Order
        {
            ReferenceNumber = "ORD-TEST-1",
            BuyerId = "buyer-1",
            SellerId = listing.SellerId,
            ListingId = listing.Id,
            Status = OrderStatus.ReadyForPickup,
            CreatedByUserId = "buyer-1",
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var result = await service.ConfirmPickupAsync(order.Id, "buyer-1");

        Assert.True(result.Succeeded);
        var reloadedOrder = await db.Orders.FindAsync(order.Id);
        Assert.Equal(OrderStatus.Completed, reloadedOrder!.Status);
        Assert.NotNull(reloadedOrder.CompletedAtUtc);

        var reloadedListing = await db.Listings.FindAsync(listing.Id);
        Assert.Equal(1, reloadedListing!.StockQuantity);
        Assert.Equal(ListingStatus.Active, reloadedListing.Status);
    }

    [Fact]
    public async Task ConfirmPickupAsync_SetsOutOfStock_WhenStockReachesZero()
    {
        var (service, db) = CreateService();
        var listing = ActiveListing(stock: 1);
        listing.Status = ListingStatus.Reserved;
        db.Listings.Add(listing);
        var order = new Order
        {
            ReferenceNumber = "ORD-TEST-2",
            BuyerId = "buyer-1",
            SellerId = listing.SellerId,
            ListingId = listing.Id,
            Status = OrderStatus.ReadyForPickup,
            CreatedByUserId = "buyer-1",
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var result = await service.ConfirmPickupAsync(order.Id, "buyer-1");

        Assert.True(result.Succeeded);
        var reloadedListing = await db.Listings.FindAsync(listing.Id);
        Assert.Equal(0, reloadedListing!.StockQuantity);
        Assert.Equal(ListingStatus.OutOfStock, reloadedListing.Status);
    }

    [Fact]
    public async Task CancelOrderAsync_ReleasesListingToActive_WithoutTouchingStock()
    {
        var (service, db) = CreateService();
        var listing = ActiveListing(stock: 2);
        listing.Status = ListingStatus.Reserved;
        db.Listings.Add(listing);
        var order = new Order
        {
            ReferenceNumber = "ORD-TEST-3",
            BuyerId = "buyer-1",
            SellerId = listing.SellerId,
            ListingId = listing.Id,
            Status = OrderStatus.Pending,
            CreatedByUserId = "buyer-1",
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var result = await service.CancelOrderAsync(order.Id, "buyer-1", "Changed my mind");

        Assert.True(result.Succeeded);
        var reloadedOrder = await db.Orders.FindAsync(order.Id);
        Assert.Equal(OrderStatus.Cancelled, reloadedOrder!.Status);
        Assert.Equal("Changed my mind", reloadedOrder.CancellationReason);

        var reloadedListing = await db.Listings.FindAsync(listing.Id);
        Assert.Equal(ListingStatus.Active, reloadedListing!.Status);
        Assert.Equal(2, reloadedListing.StockQuantity); // never debited — the sale never completed
    }

    [Fact]
    public async Task CancelOrderAsync_RequiresANonEmptyReason()
    {
        var (service, db) = CreateService();
        var listing = ActiveListing();
        listing.Status = ListingStatus.Reserved;
        db.Listings.Add(listing);
        var order = new Order
        {
            ReferenceNumber = "ORD-TEST-4",
            BuyerId = "buyer-1",
            SellerId = listing.SellerId,
            ListingId = listing.Id,
            Status = OrderStatus.Pending,
            CreatedByUserId = "buyer-1",
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var result = await service.CancelOrderAsync(order.Id, "buyer-1", "   ");

        Assert.False(result.Succeeded);
        var reloadedOrder = await db.Orders.FindAsync(order.Id);
        Assert.Equal(OrderStatus.Pending, reloadedOrder!.Status); // untouched
    }
}
