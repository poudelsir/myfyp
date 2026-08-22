using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SajhaSikshya.Data.Entities.Marketplace;
using SajhaSikshya.Data.Enums;
using SajhaSikshya.Data.ValueObjects;
using SajhaSikshya.Repositories;
using SajhaSikshya.Services.Interfaces;
using SajhaSikshya.Services.Interfaces.Catalog;
using SajhaSikshya.Services.Interfaces.Notifications;
using SajhaSikshya.Services.Marketplace;

namespace SajhaSikshya.Tests;

/// <summary>
/// Covers the stock auto-flip behavior verified live during the August 2026 audit via
/// the dedicated Restock/UpdateStockAsync path (distinct from the full Edit form, which
/// deliberately sends a listing back to PendingApproval instead — not what this class
/// tests): stock hitting zero hides a listing without admin re-review, and restocking
/// brings it straight back, plus the negative-stock guard.
/// </summary>
public class ListingStockTests
{
    private static (ListingService service, SajhaSikshya.Data.ApplicationDbContext db) CreateService()
    {
        var db = TestDbContextFactory.Create();
        // None of these are invoked by UpdateStockAsync, so unconfigured mocks are safe —
        // if that ever changes, an unconfigured Task-returning call here will fail fast
        // with a NullReferenceException rather than silently passing.
        var service = new ListingService(
            new UnitOfWork(db),
            new Mock<IImageStorageService>().Object,
            new Mock<INotificationService>().Object,
            new Mock<IUniversityService>().Object,
            NullLogger<ListingService>.Instance);

        return (service, db);
    }

    private static Listing Listing(ListingStatus status, int stock, string sellerId = "seller-1") => new()
    {
        Title = "Stock Auto-Flip Test Listing",
        Slug = "stock-auto-flip-test-listing",
        Description = "A listing used purely to exercise the stock/status auto-flip logic in tests.",
        Price = new Money(50m),
        Condition = BookCondition.Good,
        Status = status,
        StockQuantity = stock,
        SellerId = sellerId,
        CategoryId = 1,
        SubjectId = 1,
        AcademicLevelId = 1,
    };

    [Fact]
    public async Task UpdateStockAsync_ZeroStock_FlipsActiveToOutOfStock_WithNoReReview()
    {
        var (service, db) = CreateService();
        var listing = Listing(ListingStatus.Active, stock: 3);
        db.Listings.Add(listing);
        await db.SaveChangesAsync();

        var result = await service.UpdateStockAsync(listing.SellerId, listing.Id, 0);

        Assert.True(result.Succeeded);
        var reloaded = await db.Listings.FindAsync(listing.Id);
        Assert.Equal(ListingStatus.OutOfStock, reloaded!.Status); // not PendingApproval
        Assert.Equal(0, reloaded.StockQuantity);
    }

    [Fact]
    public async Task UpdateStockAsync_PositiveStock_FlipsOutOfStockBackToActive()
    {
        var (service, db) = CreateService();
        var listing = Listing(ListingStatus.OutOfStock, stock: 0);
        db.Listings.Add(listing);
        await db.SaveChangesAsync();

        var result = await service.UpdateStockAsync(listing.SellerId, listing.Id, 5);

        Assert.True(result.Succeeded);
        var reloaded = await db.Listings.FindAsync(listing.Id);
        Assert.Equal(ListingStatus.Active, reloaded!.Status);
        Assert.Equal(5, reloaded.StockQuantity);
    }

    [Fact]
    public async Task UpdateStockAsync_RejectsNegativeStock_AndLeavesStockUnchanged()
    {
        var (service, db) = CreateService();
        var listing = Listing(ListingStatus.Active, stock: 5);
        db.Listings.Add(listing);
        await db.SaveChangesAsync();

        var result = await service.UpdateStockAsync(listing.SellerId, listing.Id, -10);

        Assert.False(result.Succeeded);
        var reloaded = await db.Listings.FindAsync(listing.Id);
        Assert.Equal(5, reloaded!.StockQuantity);
        Assert.Equal(ListingStatus.Active, reloaded.Status);
    }

    [Fact]
    public async Task UpdateStockAsync_RejectsStockAboveMaximum()
    {
        var (service, db) = CreateService();
        var listing = Listing(ListingStatus.Active, stock: 5);
        db.Listings.Add(listing);
        await db.SaveChangesAsync();

        var result = await service.UpdateStockAsync(listing.SellerId, listing.Id, 100_000);

        Assert.False(result.Succeeded);
        var reloaded = await db.Listings.FindAsync(listing.Id);
        Assert.Equal(5, reloaded!.StockQuantity);
    }

    [Fact]
    public async Task UpdateStockAsync_RejectsWhenCallerDoesNotOwnTheListing()
    {
        var (service, db) = CreateService();
        var listing = Listing(ListingStatus.Active, stock: 5, sellerId: "seller-1");
        db.Listings.Add(listing);
        await db.SaveChangesAsync();

        var result = await service.UpdateStockAsync("a-different-seller", listing.Id, 10);

        Assert.False(result.Succeeded);
        var reloaded = await db.Listings.FindAsync(listing.Id);
        Assert.Equal(5, reloaded!.StockQuantity); // untouched
    }

    [Fact]
    public async Task UpdateStockAsync_RejectsWhenListingIsPendingApproval()
    {
        var (service, db) = CreateService();
        var listing = Listing(ListingStatus.PendingApproval, stock: 1);
        db.Listings.Add(listing);
        await db.SaveChangesAsync();

        var result = await service.UpdateStockAsync(listing.SellerId, listing.Id, 10);

        Assert.False(result.Succeeded);
    }
}
