using SajhaSikshya.DTOs;
using SajhaSikshya.DTOs.Catalog;
using SajhaSikshya.ViewModels.Admin.Catalog;

namespace SajhaSikshya.Services.Interfaces.Catalog;

public interface ISubjectService
{
    Task<PagedResult<SubjectDto>> GetPagedAsync(string? searchTerm, int pageNumber, int pageSize);

    Task<SubjectDto?> GetByIdAsync(int id);

    /// <summary>Active subjects for populating dropdowns (e.g. the Listing form's Subject picker).</summary>
    Task<IReadOnlyList<SubjectDto>> GetAllActiveAsync();

    Task<ServiceResult<int>> CreateAsync(SubjectFormViewModel model);

    Task<ServiceResult> UpdateAsync(SubjectFormViewModel model);

    Task<ServiceResult> DeleteAsync(int id);
}
