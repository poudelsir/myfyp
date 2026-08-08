using Microsoft.AspNetCore.Mvc;
using SajhaSikshya.Data.Enums;

namespace SajhaSikshya.ViewModels.Marketplace;

/// <summary>
/// Every filter/sort/paging input the marketplace browse page accepts, bound directly
/// from the query string (<c>[FromQuery]</c> on the controller action parameter). Each
/// property carries a short <see cref="FromQueryAttribute"/> name so URLs stay
/// bookmarkable and readable (<c>?search=anatomy&amp;category=3&amp;sort=MostViewed</c>)
/// instead of exposing the longer C# property names. Also doubles as the input to
/// <see cref="Services.Interfaces.Marketplace.IListingSearchService.SearchAsync"/> —
/// there is no separate service-layer request type, matching how
/// <c>ListingFormViewModel</c> is already passed straight into <c>IListingService</c>.
/// </summary>
public class ListingSearchCriteria
{
    [FromQuery(Name = "search")]
    public string? SearchTerm { get; set; }

    [FromQuery(Name = "category")]
    public int? CategoryId { get; set; }

    [FromQuery(Name = "university")]
    public int? UniversityId { get; set; }

    [FromQuery(Name = "academicLevel")]
    public int? AcademicLevelId { get; set; }

    [FromQuery(Name = "subject")]
    public int? SubjectId { get; set; }

    [FromQuery(Name = "condition")]
    public BookCondition? Condition { get; set; }

    [FromQuery(Name = "minPrice")]
    public decimal? MinPrice { get; set; }

    [FromQuery(Name = "maxPrice")]
    public decimal? MaxPrice { get; set; }

    [FromQuery(Name = "donation")]
    public bool DonationOnly { get; set; }

    [FromQuery(Name = "recent")]
    public bool RecentlyAddedOnly { get; set; }

    [FromQuery(Name = "hasImages")]
    public bool OnlyWithImages { get; set; }

    [FromQuery(Name = "sort")]
    public ListingSortOption Sort { get; set; } = ListingSortOption.Newest;

    [FromQuery(Name = "pageNumber")]
    public int PageNumber { get; set; } = 1;

    /// <summary>True once any actual filter narrows the result set — deliberately excludes <see cref="Sort"/>, which reorders but never shrinks it. Drives whether the marketplace homepage shows its Featured/Recent rails or a plain "Browsing: X" grid.</summary>
    public bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(SearchTerm)
        || CategoryId.HasValue
        || UniversityId.HasValue
        || AcademicLevelId.HasValue
        || SubjectId.HasValue
        || Condition.HasValue
        || MinPrice.HasValue
        || MaxPrice.HasValue
        || DonationOnly
        || RecentlyAddedOnly
        || OnlyWithImages;
}
