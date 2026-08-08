using SajhaSikshya.DTOs;
using SajhaSikshya.DTOs.Catalog;
using SajhaSikshya.ViewModels.Admin.Catalog;

namespace SajhaSikshya.Services.Interfaces.Catalog;

public interface IUniversityService
{
    Task<PagedResult<UniversityDto>> GetPagedAsync(string? searchTerm, int pageNumber, int pageSize);

    Task<UniversityDto?> GetByIdAsync(int id);

    Task<IReadOnlyList<UniversityDto>> GetAllActiveAsync();

    Task<ServiceResult<int>> CreateAsync(UniversityFormViewModel model);

    Task<ServiceResult> UpdateAsync(UniversityFormViewModel model);

    Task<ServiceResult> DeleteAsync(int id);
}
