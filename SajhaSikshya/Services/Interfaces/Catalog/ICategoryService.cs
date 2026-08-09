using SajhaSikshya.DTOs;
using SajhaSikshya.DTOs.Catalog;
using SajhaSikshya.ViewModels.Admin.Catalog;

namespace SajhaSikshya.Services.Interfaces.Catalog;

public interface ICategoryService
{
    Task<PagedResult<CategoryDto>> GetPagedAsync(string? searchTerm, int pageNumber, int pageSize);

    Task<CategoryDto?> GetByIdAsync(int id);

    /// <summary>Active categories eligible to be a parent, excluding <paramref name="excludeCategoryId"/> and its descendants (prevents cycles).</summary>
    Task<IReadOnlyList<CategoryDto>> GetEligibleParentCategoriesAsync(int? excludeCategoryId);

    /// <summary>
    /// <paramref name="categoryId"/> plus every descendant id (BFS over all non-deleted
    /// categories, active or not). Used to make selecting a top-level department match
    /// listings tagged with any of its subcategories, not just the bare department id —
    /// selecting a leaf subcategory naturally degrades to a single-id set (no descendants).
    /// </summary>
    Task<IReadOnlySet<int>> GetCategoryAndDescendantIdsAsync(int categoryId);

    /// <summary>Every active top-level department with its Active-listing count (self + subcategories) — powers the Home page's featured-department tiles.</summary>
    Task<IReadOnlyList<CategoryWithCountDto>> GetTopLevelCategoriesWithListingCountsAsync();

    Task<ServiceResult<int>> CreateAsync(CategoryFormViewModel model);

    Task<ServiceResult> UpdateAsync(CategoryFormViewModel model);

    Task<ServiceResult> DeleteAsync(int id);
}
