using SajhaSikshya.Data.Enums;

namespace SajhaSikshya.DTOs.Marketplace;

/// <summary>Presentation-safe projection of <see cref="Data.Entities.Marketplace.Listing"/>.</summary>
public class ListingDto
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal PriceAmount { get; set; }

    /// <summary>Ready-to-render price text (e.g. "Rs. 500.00" or "Free (Donation)") — see <see cref="Mappings.Marketplace.ListingMappings.ToDto"/>.</summary>
    public string FormattedPrice { get; set; } = string.Empty;

    public bool IsDonation { get; set; }

    public BookCondition Condition { get; set; }

    public string ConditionDisplay { get; set; } = string.Empty;

    public ListingStatus Status { get; set; }

    public string StatusDisplay { get; set; } = string.Empty;

    public string SellerId { get; set; } = string.Empty;

    public string SellerName { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public int SubjectId { get; set; }

    public string SubjectName { get; set; } = string.Empty;

    public string AcademicLevelName { get; set; } = string.Empty;

    public int? UniversityId { get; set; }

    public string? UniversityName { get; set; }

    public string? ThumbnailImagePath { get; set; }

    /// <summary>Ordered by <see cref="ListingImageDto.DisplayOrder"/> — see <see cref="Mappings.Marketplace.ListingMappings.ToDto"/>.</summary>
    public IReadOnlyList<ListingImageDto> Images { get; set; } = Array.Empty<ListingImageDto>();

    public int ViewCount { get; set; }

    public int FavoriteCount { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public ModerationAction? LastModerationAction { get; set; }

    public string? LastModerationActionDisplay { get; set; }

    public string? LastModeratedByName { get; set; }

    public DateTime? LastModeratedAtUtc { get; set; }

    /// <summary>Optional moderator note, most useful when <see cref="Status"/> is Rejected — shown to the seller so they know what to fix.</summary>
    public string? ModerationReason { get; set; }

    /// <summary>Whether the current viewer has saved this listing — see <see cref="ListingSummaryDto.IsSaved"/> for the same convention (stamped by the controller, not the query layer).</summary>
    public bool IsSaved { get; set; }

    /// <summary>Whether this listing is currently in the viewer's comparison list — see <see cref="ListingSummaryDto.IsInCompare"/>.</summary>
    public bool IsInCompare { get; set; }

    /// <summary>Whether <see cref="SellerId"/> currently has an approved Student Verification — see <see cref="ListingSummaryDto.IsSellerVerified"/>.</summary>
    public bool IsSellerVerified { get; set; }
}
