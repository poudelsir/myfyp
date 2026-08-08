using SajhaSikshya.Data.Enums;
using SajhaSikshya.DTOs;
using SajhaSikshya.DTOs.Reviews;

namespace SajhaSikshya.Services.Interfaces.Reviews;

/// <summary>
/// Read-only Review queries, split out from <see cref="IReviewService"/> the same way
/// every other query service is split from its command counterpart. Every method
/// returns DTOs, never tracked entities.
/// </summary>
public interface IReviewQueryService
{
    /// <summary>
    /// Reviews ABOUT <paramref name="revieweeId"/> in the given direction — "Seller
    /// Reviews" (<see cref="ReviewType.BuyerToSeller"/>) or "Buyer Reviews"
    /// (<see cref="ReviewType.SellerToBuyer"/>) on their profile, newest first,
    /// optionally narrowed to one star rating. Also serves a small "recent reviews"
    /// preview by calling with a small <paramref name="pageSize"/> — no separate method
    /// needed for that.
    /// </summary>
    Task<PagedResult<ReviewDto>> GetReviewsForUserAsync(string revieweeId, ReviewType reviewType, int? ratingFilter, int pageNumber, int pageSize);

    /// <summary>Reviews <paramref name="reviewerId"/> has WRITTEN — "My Reviews".</summary>
    Task<PagedResult<ReviewDto>> GetMyReviewsAsync(string reviewerId, int pageNumber, int pageSize);

    /// <summary>Whether <paramref name="orderId"/> already has a review in this direction — used to hide "Write a Review" once it's been used, and to block a duplicate at the UI layer before the service-level check even runs.</summary>
    Task<ReviewDto?> GetReviewForOrderAsync(int orderId, ReviewType reviewType);

    /// <summary>Full lookup by id — unrestricted by ownership (like <c>OrderQueryService.GetOrderDetailsAsync</c>); the controller checks the caller is the review's author (Edit) or an Admin (moderation) after fetching.</summary>
    Task<ReviewDto?> GetByIdAsync(int reviewId);

    /// <summary>
    /// <paramref name="userId"/>'s reputation as both seller and buyer, computed live
    /// from their current non-deleted reviews — see
    /// <see cref="Reviews.ReviewQueryService.GetReputationAsync"/>'s remarks on why this
    /// isn't a maintained/denormalized projection.
    /// </summary>
    Task<ReputationDto> GetReputationAsync(string userId);

    /// <summary>
    /// The Admin moderation queue. <paramref name="reportedOnly"/> true shows only
    /// reviews with at least one report (the common case); false additionally includes
    /// previously-removed reviews (bypassing the soft-delete filter — see
    /// <c>IGenericRepository{TEntity}.GetPagedIncludingDeletedAsync</c>) so an admin can
    /// find and Restore one.
    /// </summary>
    Task<PagedResult<ReviewDto>> GetAdminQueueAsync(bool reportedOnly, int pageNumber, int pageSize);
}
