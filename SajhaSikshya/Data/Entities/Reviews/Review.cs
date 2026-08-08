using System.ComponentModel.DataAnnotations;
using SajhaSikshya.Constants;
using SajhaSikshya.Data.Entities.Orders;
using SajhaSikshya.Data.Enums;

namespace SajhaSikshya.Data.Entities.Reviews;

/// <summary>
/// A single 1-5 star review, always tied to a completed <see cref="Order"/> — never an
/// arbitrary profile review, per Phase 9's explicit "transaction-based reviews" rule.
/// A completed order can carry at most two reviews, one per direction (see
/// <see cref="Configurations.Reviews.ReviewConfiguration"/>'s filtered unique index on
/// (OrderId, ReviewType)) — a buyer reviewing the seller and the seller reviewing the
/// buyer are two independent rows, not two sides of one row, so either party can review
/// (or not) without depending on the other having done so first.
/// <see cref="BaseEntity.CreatedAtUtc"/>/<see cref="BaseEntity.UpdatedAtUtc"/>/
/// <see cref="BaseEntity.IsDeleted"/> already satisfy the spec's matching field names —
/// nothing extra needed for any of them.
/// </summary>
public class Review : BaseEntity
{
    public int OrderId { get; set; }

    public Order Order { get; set; } = null!;

    [Required]
    public string ReviewerId { get; set; } = string.Empty;

    public ApplicationUser Reviewer { get; set; } = null!;

    [Required]
    public string RevieweeId { get; set; } = string.Empty;

    public ApplicationUser Reviewee { get; set; } = null!;

    [Range(ReviewConstants.MinimumRating, ReviewConstants.MaximumRating)]
    public int Rating { get; set; }

    /// <summary>Optional, like <see cref="Comment"/> — a star-rating-only review with no text is a valid, common review shape, so neither field is required beyond the rating itself.</summary>
    [StringLength(ReviewConstants.MaximumTitleLength)]
    public string? Title { get; set; }

    [StringLength(ReviewConstants.MaximumCommentLength)]
    public string? Comment { get; set; }

    /// <summary>Set once the author edits within <see cref="ReviewConstants.EditWindowHours"/> of posting — never reset back to false.</summary>
    public bool IsEdited { get; set; }

    /// <summary>Running count of user reports — see <see cref="Services.Interfaces.Reviews.IReviewService.ReportAsync"/>. Not a separate report-tracking table; the spec's own field list is just this counter, so "View reports" in the Admin queue means seeing this count alongside the review, not a per-reporter audit log.</summary>
    public int ReportCount { get; set; }

    public ReviewType ReviewType { get; set; }
}
