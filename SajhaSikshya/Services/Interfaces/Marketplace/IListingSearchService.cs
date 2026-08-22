using SajhaSikshya.DTOs;
using SajhaSikshya.DTOs.Marketplace;
using SajhaSikshya.ViewModels.Marketplace;

namespace SajhaSikshya.Services.Interfaces.Marketplace;

/// <summary>
/// Marketplace discovery: keyword search, filtering, sorting, and (in later Milestone 4
/// sub-steps) comparison and saved-listing retrieval. Split out from
/// <see cref="IListingQueryService"/> because discovery has fundamentally different
/// query shapes — a dynamic, criteria-driven query built up conditionally, versus the
/// fixed, purpose-built queries (browse-by-category, seller storefront, related
/// listings) that service already handles. Always restricted to
/// <see cref="Data.Enums.ListingStatus.Active"/> listings — discovery is a public-facing
/// concern, never used for the seller's own drafts or the admin moderation queue.
/// </summary>
public interface IListingSearchService
{
    /// <summary>
    /// Searches/filters/sorts Active listings per <paramref name="criteria"/> and returns
    /// one page of lightweight card-grid projections. When
    /// <see cref="ListingSearchCriteria.SearchTerm"/> is set, results are ranked by
    /// keyword relevance (see the implementation's remarks) and
    /// <see cref="ListingSearchCriteria.Sort"/> is ignored; otherwise results are ordered
    /// per <see cref="ListingSearchCriteria.Sort"/>. If every keyword must match and that
    /// finds nothing, automatically relaxes to "any keyword matches" (still
    /// relevance-ranked) rather than returning a dead end —
    /// <see cref="ListingSearchResultDto.UsedRelaxedSearch"/> tells the caller which one
    /// it actually got back.
    /// </summary>
    Task<ListingSearchResultDto> SearchAsync(ListingSearchCriteria criteria, int pageSize);

    /// <summary>
    /// The "My Saved Listings" grid: a Student's saved, still-Active listings, most
    /// recently saved first. A listing that was saved but has since gone non-Active
    /// (archived/sold/deleted by its seller, or removed by an admin) simply drops out of
    /// this list rather than showing a broken card — the underlying <c>SavedListing</c>
    /// row is untouched, so it reappears automatically if the listing becomes Active again.
    /// </summary>
    Task<PagedResult<ListingSummaryDto>> GetMySavedListingsAsync(string userId, int pageNumber, int pageSize);

    /// <summary>
    /// Full comparison-table data for exactly the given (Active-only) listings, in the
    /// same order as <paramref name="listingIds"/> — the caller (guest session list or
    /// <c>ICompareService.GetCompareListingIdsAsync</c>) already knows the right order,
    /// so this preserves it rather than imposing its own. A listing id that no longer
    /// exists or isn't Active is silently omitted, the same "just drops out" behavior as
    /// <see cref="GetMySavedListingsAsync"/>.
    /// </summary>
    Task<IReadOnlyList<ListingComparisonDto>> GetComparisonAsync(IEnumerable<int> listingIds);

    /// <summary>Whether <paramref name="listingId"/> currently exists and is Active — used to validate an Add-to-compare request (particularly the guest/session path, which has no service-layer check of its own) before it's stored.</summary>
    Task<bool> IsListingActiveAsync(int listingId);
}
