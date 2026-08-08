using SajhaSikshya.Data.Entities.Reviews;
using SajhaSikshya.DTOs.Reviews;
using SajhaSikshya.Extensions;

namespace SajhaSikshya.Mappings.Reviews;

public static class ReviewMappings
{
    /// <summary>Callers must Include Reviewer, Reviewee, and Order.Listing first — this only reads off already-loaded navigation properties, it doesn't query.</summary>
    public static ReviewDto ToDto(this Review review)
    {
        return new ReviewDto
        {
            Id = review.Id,
            OrderId = review.OrderId,
            OrderReferenceNumber = review.Order?.ReferenceNumber ?? string.Empty,
            ListingId = review.Order?.ListingId ?? 0,
            ListingTitle = review.Order?.Listing?.Title ?? string.Empty,
            ReviewerId = review.ReviewerId,
            ReviewerName = review.Reviewer?.FullName ?? string.Empty,
            ReviewerProfilePicturePath = review.Reviewer?.ProfilePicturePath,
            RevieweeId = review.RevieweeId,
            RevieweeName = review.Reviewee?.FullName ?? string.Empty,
            Rating = review.Rating,
            Title = review.Title,
            Comment = review.Comment,
            ReviewType = review.ReviewType,
            ReviewTypeDisplay = review.ReviewType.GetDisplayName(),
            IsEdited = review.IsEdited,
            ReportCount = review.ReportCount,
            IsDeleted = review.IsDeleted,
            CreatedAtUtc = review.CreatedAtUtc,
            UpdatedAtUtc = review.UpdatedAtUtc,
        };
    }
}
