using SajhaSikshya.Data.Enums;

namespace SajhaSikshya.Services.Interfaces.Orders;

/// <summary>
/// Order/Donation transaction mutations (Phase 6). Split from
/// <see cref="IOrderQueryService"/> the same way the Marketplace and Verification
/// modules split commands from queries. Every transition method validates against
/// <see cref="Orders.OrderState"/> before applying it, so an invalid transition
/// (e.g. accepting an already-completed order) fails with a clear message rather
/// than corrupting state. Verification enforcement ("only verified students may
/// create orders") is deliberately NOT duplicated here — the Buyer controller's
/// Create action carries <c>[Authorize(Policy = AuthorizationPolicies.VerifiedStudent)]</c>,
/// so by the time <see cref="CreateOrderAsync"/> runs, the caller is already known
/// to be verified; re-checking it here would duplicate a check the framework
/// already made, exactly what the project's "do not duplicate verification checks"
/// instruction warns against.
/// </summary>
public interface IOrderService
{
    /// <summary>
    /// A buyer's purchase or donation request against an Active listing. Fails if the
    /// listing isn't Active, belongs to the requesting buyer, or already has an
    /// active (Pending/Confirmed/ReadyForPickup) order against it. On success, moves
    /// the listing to <see cref="ListingStatus.Reserved"/> in the same transaction.
    /// </summary>
    Task<ServiceResult<int>> CreateOrderAsync(string buyerId, int listingId);

    /// <summary>Seller accepts a Pending request, moving it to Confirmed. Fails unless the order is Pending and the caller is its seller.</summary>
    Task<ServiceResult> AcceptOrderAsync(int orderId, string sellerId);

    /// <summary>Seller declines a Pending request — the same terminal effect as <see cref="CancelOrderAsync"/> (releases the listing back to Active), restricted to the seller and to a still-Pending order.</summary>
    Task<ServiceResult> RejectOrderAsync(int orderId, string sellerId, string reason);

    /// <summary>Seller marks a Confirmed order ready for the buyer to collect, optionally recording where/when. Fails unless the order is Confirmed and the caller is its seller.</summary>
    Task<ServiceResult> MarkReadyForPickupAsync(int orderId, string sellerId, string? pickupNotes = null);

    /// <summary>
    /// Buyer confirms they've collected the item, completing the order. On success,
    /// decrements the listing's stock and moves it to <see cref="ListingStatus.Active"/>
    /// (stock remains) or <see cref="ListingStatus.OutOfStock"/> (stock reaches zero) —
    /// or <see cref="ListingStatus.Donated"/> for a donation, which stays terminal —
    /// all in the same transaction. Fails unless the order is ReadyForPickup and the
    /// caller is its buyer.
    /// </summary>
    Task<ServiceResult> ConfirmPickupAsync(int orderId, string buyerId);

    /// <summary>Buyer or seller cancels a Pending or Confirmed order, releasing the listing back to Active. Fails unless the caller is a party to the order and the order hasn't progressed past Confirmed.</summary>
    Task<ServiceResult> CancelOrderAsync(int orderId, string userId, string reason);

    /// <summary>Unrestricted admin cancellation (dispute resolution) — same mechanics as <see cref="CancelOrderAsync"/>, no ownership check.</summary>
    Task<ServiceResult> AdminCancelOrderAsync(int orderId, string adminUserId, string reason);
}
