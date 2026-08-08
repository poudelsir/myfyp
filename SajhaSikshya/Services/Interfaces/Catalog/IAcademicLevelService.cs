using SajhaSikshya.DTOs;
using SajhaSikshya.DTOs.Catalog;
using SajhaSikshya.ViewModels.Admin.Catalog;

namespace SajhaSikshya.Services.Interfaces.Catalog;

public interface IAcademicLevelService
{
    Task<PagedResult<AcademicLevelDto>> GetPagedAsync(string? searchTerm, int pageNumber, int pageSize);

    Task<AcademicLevelDto?> GetByIdAsync(int id);

    Task<IReadOnlyList<AcademicLevelDto>> GetAllActiveAsync();

    Task<ServiceResult<int>> CreateAsync(AcademicLevelFormViewModel model);

    Task<ServiceResult> UpdateAsync(AcademicLevelFormViewModel model);

    Task<ServiceResult> DeleteAsync(int id);
}
