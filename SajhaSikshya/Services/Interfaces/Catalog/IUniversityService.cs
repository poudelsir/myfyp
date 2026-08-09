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

    /// <summary>
    /// Resolves a seller-typed university name to an existing University (case-insensitive
    /// match, reactivating it if an admin had deactivated it) or creates a new one with an
    /// auto-generated <see cref="Data.Entities.Catalog.University.Code"/> — the listing
    /// form's "type your university, we'll add it if it's new" field. Distinct from the
    /// Admin CRUD in <see cref="CreateAsync"/>, which requires an admin-chosen Code and
    /// rejects a duplicate outright rather than reusing it.
    /// </summary>
    Task<int> FindOrCreateByNameAsync(string name);
}
