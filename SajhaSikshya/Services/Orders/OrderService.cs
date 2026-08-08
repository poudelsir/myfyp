using SajhaSikshya.Data.Entities.Marketplace;
using SajhaSikshya.Data.Entities.Orders;
using SajhaSikshya.Data.Enums;
using SajhaSikshya.Repositories.Interfaces;
using SajhaSikshya.Services.Interfaces.Notifications;
using SajhaSikshya.Services.Interfaces.Orders;
using SajhaSikshya.Services.Notifications;

namespace SajhaSikshya.Services.Orders;

/// <summary>
/// Implements <see cref="IOrderService"/>. Every transition (including creation)
/// appends a new <see cref="OrderStatusHistory"/> row via the order's own
/// <c>StatusHistory</c> navigation collection rather than a separate insert — EF
/// Core resolves the child's OrderId automatically once the parent is saved, so the
/// whole operation (order + history + the paired Listing status change) commits in
/// one <see cref="IUnitOfWork.SaveChangesAsync"/> call, atomically. History rows are
/// never updated or removed after being added — <see cref="Order"/> itself always
/// reflects the *current* state, and <c>OrderStatusHistory</c> is the permanent,
/// append-only trail of how it got there, mirroring how
/// <c>StudentVerification</c>'s history is a full row per submission rather than a
/// single mutable "last state".
///
/// Each transition also notifies the party who didn't just perform it (Phase 8.2) via
/// <see cref="_notificationService"/> — Create notifies the seller, Accept/MarkReady/
/// ConfirmPickup notify the buyer, and cancellation notifies everyone except whoever
/// triggered it (which naturally covers Reject, a buyer-or-seller Cancel, AND
/// AdminCancel with the same single code path: an admin's id matches neither buyer
/// nor seller, so both get notified without any special-casing for that one caller).
/// </summary>
public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IUnitOfWork unitOfWork, INotificationService notificationService, ILogger<OrderService> logger)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<ServiceResult<int>> CreateOrderAsync(string buyerId, int listingId)
    {
        var listingRepository = _unitOfWork.Repository<Listing>();
        var listing = await listingRepository.GetByIdAsync(listingId);
        if (listing is null || listing.Status != ListingStatus.Active)
        {
            return ServiceResult<int>.Failure("This listing is not available to order.");
        }

        if (listing.SellerId == buyerId)
        {
            return ServiceResult<int>.Failure("You cannot order your own listing.");
        }

        var orderRepository = _unitOfWork.Repository<Order>();
        var hasActiveOrder = await orderRepository.AnyAsync(o =>
            o.ListingId == listingId
            && (o.Status == OrderStatus.Pending || o.Status == OrderStatus.Confirmed || o.Status == OrderStatus.ReadyForPickup));

        if (hasActiveOrder)
        {
            return ServiceResult<int>.Failure("This listing already has an active order in progress.");
        }

        var order = new Order
        {
            BuyerId = buyerId,
            SellerId = listing.SellerId,
            ListingId = listingId,
            Status = OrderStatus.Pending,
            IsDonation = listing.IsDonation,
            CreatedByUserId = buyerId,
        };

        order.StatusHistory.Add(new OrderStatusHistory
        {
            OldStatus = null,
            NewStatus = OrderStatus.Pending,
            ChangedByUserId = buyerId,
            ChangedAtUtc = DateTime.UtcNow,
        });

        await orderRepository.AddAsync(order);

        listing.Status = ListingStatus.Reserved;
        listingRepository.Update(listing);

        await _unitOfWork.SaveChangesAsync();

        // ReferenceNumber embeds Id, which SQL Server only assigns once the row above is
        // saved — so it's written in this second, small save rather than the first.
        order.ReferenceNumber = $"ORD-{order.CreatedAtUtc.Year}-{order.Id:D6}";
        order.PaymentMethod = order.IsDonation ? PaymentMethod.Unknown : PaymentMethod.CashOnPickup;
        orderRepository.Update(order);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Order {OrderId} ({ReferenceNumber}) created for listing {ListingId} by buyer {BuyerId}", order.Id, order.ReferenceNumber, listingId, buyerId);

        var (createdTitle, createdMessage) = NotificationTemplates.OrderCreated(listing.Title);
        await _notificationService.CreateAsync(listing.SellerId, NotificationType.Order, createdTitle, createdMessage, OrderLink(order.Id), createdBy: buyerId);

        return ServiceResult<int>.Success(order.Id);
    }

    public async Task<ServiceResult> AcceptOrderAsync(int orderId, string sellerId)
    {
        var repository = _unitOfWork.Repository<Order>();
        var order = await repository.GetByIdAsync(orderId);
        if (order is null || order.SellerId != sellerId)
        {
            return ServiceResult.Failure("Order not found.");
        }

        if (!OrderState.CanTransition(order.Status, OrderStatus.Confirmed))
        {
            return ServiceResult.Failure("This order can no longer be accepted.");
        }

        ApplyTransition(order, OrderStatus.Confirmed, sellerId, null);
        order.ConfirmedAtUtc = DateTime.UtcNow;

        repository.Update(order);
        await _unitOfWork.SaveChangesAsync();

        var listingTitle = await GetListingTitleAsync(order.ListingId);
        var (confirmedTitle, confirmedMessage) = NotificationTemplates.OrderConfirmed(listingTitle);
        await _notificationService.CreateAsync(order.BuyerId, NotificationType.Order, confirmedTitle, confirmedMessage, OrderLink(order.Id), createdBy: sellerId);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> RejectOrderAsync(int orderId, string sellerId, string reason)
    {
        var repository = _unitOfWork.Repository<Order>();
        var order = await repository.GetByIdAsync(orderId);
        if (order is null || order.SellerId != sellerId)
        {
            return ServiceResult.Failure("Order not found.");
        }

        return await CancelInternalAsync(order, sellerId, reason);
    }

    public async Task<ServiceResult> MarkReadyForPickupAsync(int orderId, string sellerId, string? pickupNotes = null)
    {
        var repository = _unitOfWork.Repository<Order>();
        var order = await repository.GetByIdAsync(orderId);
        if (order is null || order.SellerId != sellerId)
        {
            return ServiceResult.Failure("Order not found.");
        }

        if (!OrderState.CanTransition(order.Status, OrderStatus.ReadyForPickup))
        {
            return ServiceResult.Failure("This order is not ready to be marked for pickup.");
        }

        ApplyTransition(order, OrderStatus.ReadyForPickup, sellerId, null);
        order.PickupNotes = string.IsNullOrWhiteSpace(pickupNotes) ? null : pickupNotes.Trim();

        repository.Update(order);
        await _unitOfWork.SaveChangesAsync();

        var listingTitle = await GetListingTitleAsync(order.ListingId);
        var (readyTitle, readyMessage) = NotificationTemplates.OrderReadyForPickup(listingTitle);
        await _notificationService.CreateAsync(order.BuyerId, NotificationType.Order, readyTitle, readyMessage, OrderLink(order.Id), createdBy: sellerId);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> ConfirmPickupAsync(int orderId, string buyerId)
    {
        var repository = _unitOfWork.Repository<Order>();
        var order = await repository.GetByIdAsync(orderId);
        if (order is null || order.BuyerId != buyerId)
        {
            return ServiceResult.Failure("Order not found.");
        }

        if (!OrderState.CanTransition(order.Status, OrderStatus.Completed))
        {
            return ServiceResult.Failure("This order is not ready for pickup confirmation.");
        }

        ApplyTransition(order, OrderStatus.Completed, buyerId, null);
        order.CompletedAtUtc = DateTime.UtcNow;
        repository.Update(order);

        var listingRepository = _unitOfWork.Repository<Listing>();
        var listing = await listingRepository.GetByIdAsync(order.ListingId);
        if (listing is not null)
        {
            listing.Status = order.IsDonation ? ListingStatus.Donated : ListingStatus.Sold;
            listingRepository.Update(listing);
        }

        await _unitOfWork.SaveChangesAsync();

        var (completedTitle, completedMessage) = NotificationTemplates.OrderCompleted(listing?.Title ?? "your listing");
        await _notificationService.CreateAsync(order.SellerId, NotificationType.Order, completedTitle, completedMessage, OrderLink(order.Id), createdBy: buyerId);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> CancelOrderAsync(int orderId, string userId, string reason)
    {
        var repository = _unitOfWork.Repository<Order>();
        var order = await repository.GetByIdAsync(orderId);
        if (order is null || (order.BuyerId != userId && order.SellerId != userId))
        {
            return ServiceResult.Failure("Order not found.");
        }

        return await CancelInternalAsync(order, userId, reason);
    }

    public async Task<ServiceResult> AdminCancelOrderAsync(int orderId, string adminUserId, string reason)
    {
        var repository = _unitOfWork.Repository<Order>();
        var order = await repository.GetByIdAsync(orderId);
        if (order is null)
        {
            return ServiceResult.Failure("Order not found.");
        }

        return await CancelInternalAsync(order, adminUserId, reason);
    }

    /// <summary>Shared cancel/reject mechanics for the seller-owned Reject, the buyer-or-seller Cancel, and the unrestricted AdminCancel paths — kept in one place so the "still Pending or Confirmed" guard and the listing-release logic can't drift out of sync between them.</summary>
    private async Task<ServiceResult> CancelInternalAsync(Order order, string changedByUserId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return ServiceResult.Failure("Please provide a reason.");
        }

        if (!OrderState.CanTransition(order.Status, OrderStatus.Cancelled))
        {
            return ServiceResult.Failure("This order can no longer be cancelled.");
        }

        var trimmedReason = reason.Trim();
        ApplyTransition(order, OrderStatus.Cancelled, changedByUserId, trimmedReason);
        order.CancelledAtUtc = DateTime.UtcNow;
        order.CancellationReason = trimmedReason;

        _unitOfWork.Repository<Order>().Update(order);
        await ReleaseListingAsync(order.ListingId);
        await _unitOfWork.SaveChangesAsync();

        // Notifies whichever party did NOT trigger the cancellation. For Reject/Cancel
        // that's always exactly one party (the other one); for AdminCancel,
        // changedByUserId is the admin's id, which matches neither buyer nor seller, so
        // this naturally notifies both without any special-casing for that caller.
        var listingTitle = await GetListingTitleAsync(order.ListingId);
        var (cancelledTitle, cancelledMessage) = NotificationTemplates.OrderCancelled(listingTitle, trimmedReason);
        var link = OrderLink(order.Id);
        if (order.BuyerId != changedByUserId)
        {
            await _notificationService.CreateAsync(order.BuyerId, NotificationType.Order, cancelledTitle, cancelledMessage, link, createdBy: changedByUserId);
        }

        if (order.SellerId != changedByUserId)
        {
            await _notificationService.CreateAsync(order.SellerId, NotificationType.Order, cancelledTitle, cancelledMessage, link, createdBy: changedByUserId);
        }

        return ServiceResult.Success();
    }

    /// <summary>The listing's title for notification text, falling back to a generic phrase in the (never-expected) case it's gone — small enough, and used often enough across every transition method, to be worth its own helper rather than repeating the fetch-and-fallback at each call site.</summary>
    private async Task<string> GetListingTitleAsync(int listingId)
    {
        var listing = await _unitOfWork.Repository<Listing>().GetByIdAsync(listingId);
        return listing?.Title ?? "your listing";
    }

    private static string OrderLink(int orderId) => $"/Student/Orders/Details/{orderId}";

    /// <summary>Sends a listing back to Active once its in-progress order is cancelled/rejected — guarded on it actually being Reserved so this never clobbers a status a different code path already moved on.</summary>
    private async Task ReleaseListingAsync(int listingId)
    {
        var listingRepository = _unitOfWork.Repository<Listing>();
        var listing = await listingRepository.GetByIdAsync(listingId);
        if (listing is not null && listing.Status == ListingStatus.Reserved)
        {
            listing.Status = ListingStatus.Active;
            listingRepository.Update(listing);
        }
    }

    /// <summary>Mutates <paramref name="order"/>'s Status and appends the matching history row — the one place every transition method funnels through, so a status change and its audit record can never happen independently of one another.</summary>
    private static void ApplyTransition(Order order, OrderStatus newStatus, string changedByUserId, string? reason)
    {
        var oldStatus = order.Status;
        order.Status = newStatus;
        order.StatusHistory.Add(new OrderStatusHistory
        {
            OldStatus = oldStatus,
            NewStatus = newStatus,
            ChangedByUserId = changedByUserId,
            ChangedAtUtc = DateTime.UtcNow,
            Reason = reason,
        });
    }
}
