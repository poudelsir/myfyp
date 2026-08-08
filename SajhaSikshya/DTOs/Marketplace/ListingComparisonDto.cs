using SajhaSikshya.Data.Enums;

namespace SajhaSikshya.DTOs.Marketplace;

/// <summary>Dedicated projection for the side-by-side comparison table — deliberately not <see cref="ListingSummaryDto"/> or <see cref="ListingDto"/>, since the comparison page needs a different, comparison-specific set of fields (Academic Level, full Created Date) and none of the card/detail-page-only ones (Slug is kept only to link back, Description/Images are not shown at all).</summary>
public class ListingComparisonDto
{
    public int Id { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? ThumbnailImagePath { get; set; }

    public decimal PriceAmount { get; set; }

    /// <summary>Ready-to-render price text — see <see cref="Mappings.Marketplace.ListingMappings.ApplyComparisonDisplayFields"/>.</summary>
    public string FormattedPrice { get; set; } = string.Empty;

    public bool IsDonation { get; set; }

    public BookCondition Condition { get; set; }

    public string ConditionDisplay { get; set; } = string.Empty;

    public string? UniversityName { get; set; }

    public string AcademicLevelName { get; set; } = string.Empty;

    public string SubjectName { get; set; } = string.Empty;

    public string SellerName { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public int ViewCount { get; set; }
}
