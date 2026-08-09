using SajhaSikshya.Data.Enums;
using SajhaSikshya.DTOs;
using SajhaSikshya.DTOs.Marketplace;

namespace SajhaSikshya.Services.Interfaces.Marketplace;

/// <summary>
/// Read-only Listing queries, split out from <see cref="IListingService"/> so the
/// growing set of mutation concerns there (create/edit/archive/image management)
/// doesn't keep accumulating read paths too. Every method here returns DTOs (never
/// tracked entities) and is safe to call as often as a page needs without touching
/// the change tracker. Seller-scoped, public-unrestricted, and admin-unrestricted
/// methods all sit side by side here — the distinction is entirely in what each
/// method's own filter enforces, not in separate services.
/// </summary>
public interface IListingQueryService
{
    Task<PagedResult<ListingDto>> GetMyListingsAsync(string sellerId, string? searchTerm, ListingStatus? status, int? categoryId, int pageNumber, int pageSize);

    /// <summary>Used by the Edit form, Preview page, and the image-management panel's redisplay-on-error path — returns null if the listing doesn't exist or isn't owned by <paramref name="sellerId"/>.</summary>
    Task<ListingDto?> GetForSellerAsync(string sellerId, int id);

    /// <summary>The most-viewed Active listings, for the marketplace homepage's "Featured" rail.</summary>
    Task<IReadOnlyList<ListingSummaryDto>> GetFeaturedListingsAsync(int count);

    /// <summary>The most recently created Active listings, for the marketplace homepage's "Recent" rail.</summary>
    Task<IReadOnlyList<ListingSummaryDto>> GetRecentListingsAsync(int count);

    /// <summary>Full detail lookup for the public details page. Only returns a result if the listing is Active — Draft/PendingApproval/Rejected/Archived listings are never publicly visible, even by direct slug.</summary>
    Task<ListingDto?> GetPublicDetailsBySlugAsync(string slug);

    /// <summary>Other Active listings for the same Subject, excluding <paramref name="listingId"/> itself.</summary>
    Task<IReadOnlyList<ListingSummaryDto>> GetRelatedListingsAsync(int listingId, int count);

    /// <summary>A seller's public storefront: their Active listings only (never Draft/Pending/Rejected/Archived, regardless of who's asking).</summary>
    Task<PagedResult<ListingSummaryDto>> GetPublicSellerListingsAsync(string sellerId, int pageNumber, int pageSize);

    /// <summary>Seller summary (name, member-since, active listing count) for the details page's seller card and the seller's own storefront header. Null if the account doesn't exist or is deactivated.</summary>
    Task<SellerProfileDto?> GetSellerProfileAsync(string sellerId);

    /// <summary>The Admin moderation queue/list: every listing regardless of status or owner, optionally narrowed by title search and/or status.</summary>
    Task<PagedResult<ListingDto>> GetAllForAdminAsync(string? searchTerm, ListingStatus? status, int pageNumber, int pageSize);

    /// <summary>Unrestricted single-listing lookup by id (not slug, not Active-only) — admin needs to review a PendingApproval listing's full details before deciding, which the public-facing GetPublicDetailsBySlugAsync would refuse to return.</summary>
    Task<ListingDto?> GetForAdminAsync(int listingId);

    /// <summary>Aggregate counts for the Admin Dashboard.</summary>
    Task<ListingModerationStatsDto> GetModerationStatsAsync();
}
