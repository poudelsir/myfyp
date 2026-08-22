namespace SajhaSikshya.DTOs.Marketplace;

/// <summary>
/// Wraps <c>IListingSearchService.SearchAsync</c>'s page of results with whether
/// the strict "every keyword must match" search actually found anything. When it
/// didn't, <see cref="Services.Marketplace.ListingSearchService"/> automatically
/// relaxes to "any keyword matches" (still relevance-ranked) rather than showing the
/// buyer a dead end — <see cref="UsedRelaxedSearch"/> is <c>true</c> exactly when that
/// fallback is what's actually being shown, so the Browse page can label the results
/// "Showing related results" instead of silently pretending they were an exact match.
/// </summary>
public class ListingSearchResultDto
{
    public PagedResult<ListingSummaryDto> Page { get; set; } = new();

    public bool UsedRelaxedSearch { get; set; }
}
