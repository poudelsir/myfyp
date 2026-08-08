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

    Task<ServiceResult<int>> CreateAsync(CategoryFormViewModel model);

    Task<ServiceResult> UpdateAsync(CategoryFormViewModel model);

    Task<ServiceResult> DeleteAsync(int id);
}
