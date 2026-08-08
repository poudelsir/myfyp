using SajhaSikshya.DTOs;
using SajhaSikshya.DTOs.Catalog;
using SajhaSikshya.DTOs.Marketplace;

namespace SajhaSikshya.ViewModels.Marketplace;

/// <summary>
/// Backs the public marketplace browse/search page (<c>/marketplace</c>).
/// <see cref="Featured"/> and <see cref="Recent"/> are only populated on the unfiltered
/// first page — once any filter/search term is active (<see cref="IsFiltered"/>) or a
/// later page is requested, the page shows just <see cref="Browse"/> under a
/// "Browsing: X" / "Search results" heading instead of the homepage rails.
/// </summary>
public class MarketplaceHomeViewModel
{
    public IReadOnlyList<ListingSummaryDto> Featured { get; set; } = Array.Empty<ListingSummaryDto>();

    public IReadOnlyList<ListingSummaryDto> Recent { get; set; } = Array.Empty<ListingSummaryDto>();

    public PagedResult<ListingSummaryDto> Browse { get; set; } = new();

    /// <summary>Every bound filter/sort/paging input for this request — also what gets echoed back into the search bar, filter sidebar, and sort dropdown so the page reflects the URL it was loaded from.</summary>
    public ListingSearchCriteria Criteria { get; set; } = new();

    public string? CategoryName { get; set; }

    public string? SubjectName { get; set; }

    public string? UniversityName { get; set; }

    public string? AcademicLevelName { get; set; }

    /// <summary>True once any actual filter or search term narrows the result set — see <see cref="ListingSearchCriteria.HasActiveFilters"/>.</summary>
    public bool IsFiltered => Criteria.HasActiveFilters;

    /// <summary>For the category filter dropdown.</summary>
    public IReadOnlyList<CategoryDto> BrowseCategories { get; set; } = Array.Empty<CategoryDto>();

    /// <summary>For the university filter dropdown.</summary>
    public IReadOnlyList<UniversityDto> BrowseUniversities { get; set; } = Array.Empty<UniversityDto>();

    /// <summary>For the academic level filter dropdown.</summary>
    public IReadOnlyList<AcademicLevelDto> BrowseAcademicLevels { get; set; } = Array.Empty<AcademicLevelDto>();

    /// <summary>For the subject filter dropdown.</summary>
    public IReadOnlyList<SubjectDto> BrowseSubjects { get; set; } = Array.Empty<SubjectDto>();
}
