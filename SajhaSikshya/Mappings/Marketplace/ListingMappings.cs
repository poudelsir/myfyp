using System.Linq.Expressions;
using SajhaSikshya.Data.Entities.Marketplace;
using SajhaSikshya.Data.ValueObjects;
using SajhaSikshya.DTOs.Marketplace;
using SajhaSikshya.Extensions;

namespace SajhaSikshya.Mappings.Marketplace;

public static class ListingMappings
{
    /// <summary>
    /// Shared projection for every card-grid query across <c>ListingQueryService</c> and
    /// <c>ListingSearchService</c>. Every member access here must stay SQL-translatable
    /// (property chains, ternary null-checks, string concatenation) — no method calls —
    /// so EF Core can turn this into one query with JOINs instead of loading full entity
    /// graphs. Display-formatted fields are filled in afterward by
    /// <see cref="ApplyDisplayFields"/>, over the materialized (in-memory) results.
    /// </summary>
    public static readonly Expression<Func<Listing, ListingSummaryDto>> ToSummaryProjection = l => new ListingSummaryDto
    {
        Id = l.Id,
        Title = l.Title,
        Slug = l.Slug,
        PriceAmount = l.Price.Amount,
        IsDonation = l.IsDonation,
        Condition = l.Condition,
        CategoryName = l.Category.Name,
        UniversityName = l.University != null ? l.University.Name : null,
        SellerId = l.SellerId,
        SellerName = l.Seller.FirstName + " " + l.Seller.LastName,
        ThumbnailImagePath = l.ThumbnailImage != null ? l.ThumbnailImage.ImagePath : null,
        ViewCount = l.ViewCount,
        CreatedAtUtc = l.CreatedAtUtc,
    };

    /// <summary>Fills the formatting-dependent fields that can't be expressed in a SQL-translatable projection (enum display text, currency formatting) — runs over already-materialized rows.</summary>
    public static void ApplyDisplayFields(IEnumerable<ListingSummaryDto> items)
    {
        foreach (var item in items)
        {
            item.ConditionDisplay = item.Condition.GetDisplayName();
            item.FormattedPrice = item.IsDonation ? "Free (Donation)" : new Money(item.PriceAmount).ToString();
        }
    }

    /// <summary>Dedicated projection for the comparison table (Milestone 4.3) — a different field set than <see cref="ToSummaryProjection"/> (Academic Level instead of Category, no Slug-adjacent card fields), so kept separate rather than reusing it. Same SQL-translatability rules apply; formatting is filled in afterward by <see cref="ApplyComparisonDisplayFields"/>.</summary>
    public static readonly Expression<Func<Listing, ListingComparisonDto>> ToComparisonProjection = l => new ListingComparisonDto
    {
        Id = l.Id,
        Slug = l.Slug,
        Title = l.Title,
        ThumbnailImagePath = l.ThumbnailImage != null ? l.ThumbnailImage.ImagePath : null,
        PriceAmount = l.Price.Amount,
        IsDonation = l.IsDonation,
        Condition = l.Condition,
        UniversityName = l.University != null ? l.University.Name : null,
        AcademicLevelName = l.AcademicLevel.Name,
        SubjectName = l.Subject.Name,
        SellerName = l.Seller.FirstName + " " + l.Seller.LastName,
        CreatedAtUtc = l.CreatedAtUtc,
        ViewCount = l.ViewCount,
    };

    /// <summary>Comparison-table counterpart to <see cref="ApplyDisplayFields"/>.</summary>
    public static void ApplyComparisonDisplayFields(IEnumerable<ListingComparisonDto> items)
    {
        foreach (var item in items)
        {
            item.ConditionDisplay = item.Condition.GetDisplayName();
            item.FormattedPrice = item.IsDonation ? "Free (Donation)" : new Money(item.PriceAmount).ToString();
        }
    }

    /// <summary>
    /// Callers must Include Seller/Category/Subject/AcademicLevel/University/ThumbnailImage/Images
    /// first — this only reads off already-loaded navigation properties, it doesn't query.
    /// </summary>
    public static ListingDto ToDto(this Listing listing)
    {
        return new ListingDto
        {
            Id = listing.Id,
            Title = listing.Title,
            Slug = listing.Slug,
            Description = listing.Description,
            PriceAmount = listing.Price.Amount,
            FormattedPrice = listing.IsDonation ? "Free (Donation)" : listing.Price.ToString(),
            IsDonation = listing.IsDonation,
            Condition = listing.Condition,
            ConditionDisplay = listing.Condition.GetDisplayName(),
            Status = listing.Status,
            StatusDisplay = listing.Status.GetDisplayName(),
            SellerId = listing.SellerId,
            SellerName = listing.Seller?.FullName ?? string.Empty,
            CategoryId = listing.CategoryId,
            CategoryName = listing.Category?.Name ?? string.Empty,
            SubjectId = listing.SubjectId,
            SubjectName = listing.Subject?.Name ?? string.Empty,
            AcademicLevelName = listing.AcademicLevel?.Name ?? string.Empty,
            UniversityId = listing.UniversityId,
            UniversityName = listing.University?.Name,
            ThumbnailImagePath = listing.ThumbnailImage?.ImagePath,
            Images = listing.Images?.OrderBy(i => i.DisplayOrder).Select(i => i.ToDto()).ToList() ?? new List<ListingImageDto>(),
            ViewCount = listing.ViewCount,
            FavoriteCount = listing.FavoriteCount,
            CreatedAtUtc = listing.CreatedAtUtc,
            LastModerationAction = listing.LastModerationAction,
            LastModerationActionDisplay = listing.LastModerationAction?.GetDisplayName(),
            LastModeratedByName = listing.LastModeratedBy?.FullName,
            LastModeratedAtUtc = listing.LastModeratedAtUtc,
            ModerationReason = listing.ModerationReason,
        };
    }
}
