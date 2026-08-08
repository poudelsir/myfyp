using SajhaSikshya.Data.Enums;

namespace SajhaSikshya.DTOs.Reviews;

public class ReviewDto
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public string OrderReferenceNumber { get; set; } = string.Empty;

    public int ListingId { get; set; }

    public string ListingTitle { get; set; } = string.Empty;

    public string ReviewerId { get; set; } = string.Empty;

    public string ReviewerName { get; set; } = string.Empty;

    public string? ReviewerProfilePicturePath { get; set; }

    public string RevieweeId { get; set; } = string.Empty;

    public string RevieweeName { get; set; } = string.Empty;

    public int Rating { get; set; }

    public string? Title { get; set; }

    public string? Comment { get; set; }

    public ReviewType ReviewType { get; set; }

    public string ReviewTypeDisplay { get; set; } = string.Empty;

    public bool IsEdited { get; set; }

    public int ReportCount { get; set; }

    /// <summary>Only ever true in the Admin moderation queue — every other query path filters soft-deleted rows out before they can become a DTO.</summary>
    public bool IsDeleted { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}
