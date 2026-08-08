using SajhaSikshya.Data.Enums;

namespace SajhaSikshya.Services.Interfaces.Reviews;

/// <summary>
/// Review mutations (Phase 9). Split from <see cref="IReviewQueryService"/> the same
/// way every other module in this project separates commands from queries. Every
/// method re-derives ownership/participancy from the database rather than trusting a
/// caller-supplied id — a review can only ever be created for an order the caller was
/// actually a party to, and only ever edited/deleted by the person who wrote it.
/// </summary>
public interface IReviewService
{
    /// <summary>
    /// Creates a review for a completed order. <paramref name="reviewerId"/> must be
    /// either the order's buyer or seller; the review direction
    /// (<see cref="ReviewType"/>) and reviewee are derived automatically from which one
    /// they are — never supplied directly, so a caller can't forge a review "from" the
    /// other party. Fails if the order isn't <see cref="Data.Enums.OrderStatus.Completed"/>,
    /// the reviewer wasn't a party to it, or that direction has already been reviewed
    /// (see the filtered unique index on (OrderId, ReviewType)). Notifies the reviewee.
    /// </summary>
    Task<ServiceResult<int>> CreateAsync(int orderId, string reviewerId, int rating, string? title, string? comment);

    /// <summary>Edits the caller's own review. Fails unless they're its author, it's within <see cref="Constants.ReviewConstants.EditWindowHours"/> of posting, and it isn't deleted.</summary>
    Task<ServiceResult> UpdateAsync(int reviewId, string reviewerId, int rating, string? title, string? comment);

    /// <summary>Soft-deletes the caller's own review. No notification — a self-initiated action needs no announcement.</summary>
    Task<ServiceResult> DeleteAsync(int reviewId, string reviewerId);

    /// <summary>
    /// Any authenticated user flags a review as inappropriate — incrementing its
    /// <see cref="Data.Entities.Reviews.Review.ReportCount"/> and notifying every
    /// Administrator (via <see cref="Notifications.INotificationService.CreateBroadcastAsync"/>,
    /// reusing the exact broadcast mechanism Phase 8 built) that the moderation queue
    /// needs attention. Fails if the caller is reporting their own review.
    /// </summary>
    Task<ServiceResult> ReportAsync(int reviewId, string reporterId);

    /// <summary>
    /// Admin moderation — Remove (soft-delete, notifies the author with an optional
    /// reason), Restore (undoes a Remove), or ResetReportCount (clears unfounded
    /// reports) — mirrors <c>ListingService.ModerateListingAsync</c>'s single-method,
    /// action-enum shape.
    /// </summary>
    Task<ServiceResult> ModerateAsync(int reviewId, ReviewModerationAction action, string adminId, string? reason = null);
}
