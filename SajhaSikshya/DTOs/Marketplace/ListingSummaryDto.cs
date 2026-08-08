using SajhaSikshya.Data.Enums;

namespace SajhaSikshya.DTOs.Marketplace;

/// <summary>
/// Lightweight projection of a Listing for card-grid display (marketplace browse,
/// featured, recent, related, seller listings). Deliberately not <see cref="ListingDto"/>
/// — a card needs only these columns, so <c>ListingQueryService</c> selects straight into
/// this shape at the SQL level instead of materializing full Listing entity graphs.
/// <see cref="FormattedPrice"/>/<see cref="ConditionDisplay"/> are filled in by the query
/// service after materialization (formatting logic isn't SQL-translatable), not by the
/// projection itself.
/// </summary>
public class ListingSummaryDto
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public decimal PriceAmount { get; set; }

    public string FormattedPrice { get; set; } = string.Empty;

    public bool IsDonation { get; set; }

    public BookCondition Condition { get; set; }

    public string ConditionDisplay { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public string? UniversityName { get; set; }

    public string SellerId { get; set; } = string.Empty;

    public string SellerName { get; set; } = string.Empty;

    public string? ThumbnailImagePath { get; set; }

    public int ViewCount { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Whether the current viewer has saved this listing. Always <c>false</c> straight
    /// out of a query/projection — there's no ambient "current user" at that layer — and
    /// is stamped afterward by the controller (via <c>ISavedListingService.GetSavedListingIdsAsync</c>)
    /// for authenticated requests only. Guests always see <c>false</c>.
    /// </summary>
    public bool IsSaved { get; set; }

    /// <summary>Whether this listing is currently in the viewer's comparison list (guest session or, for authenticated users, the database). Same stamped-by-the-controller convention as <see cref="IsSaved"/>.</summary>
    public bool IsInCompare { get; set; }

    /// <summary>Whether <see cref="SellerId"/> currently has an approved Student Verification. Same stamped-by-the-controller convention as <see cref="IsSaved"/> (via <c>IVerificationQueryService.GetVerifiedUserIdsAsync</c>) — about the seller, not the viewer, so it's visible to guests too.</summary>
    public bool IsSellerVerified { get; set; }
}
