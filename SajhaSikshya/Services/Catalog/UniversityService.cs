using Microsoft.EntityFrameworkCore;
using SajhaSikshya.Data.Entities.Catalog;
using SajhaSikshya.DTOs;
using SajhaSikshya.DTOs.Catalog;
using SajhaSikshya.Mappings.Catalog;
using SajhaSikshya.Repositories.Interfaces;
using SajhaSikshya.Services.Interfaces.Catalog;
using SajhaSikshya.ViewModels.Admin.Catalog;

namespace SajhaSikshya.Services.Catalog;

public class UniversityService : IUniversityService
{
    private readonly IUnitOfWork _unitOfWork;

    public UniversityService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<UniversityDto>> GetPagedAsync(string? searchTerm, int pageNumber, int pageSize)
    {
        var repository = _unitOfWork.Repository<University>();

        var page = await repository.GetPagedAsync(
            pageNumber,
            pageSize,
            filter: string.IsNullOrWhiteSpace(searchTerm)
                ? null
                : u => u.Name.Contains(searchTerm) || u.Code.Contains(searchTerm),
            orderBy: q => q.OrderBy(u => u.Name),
            include: q => q.Include(u => u.Subjects));

        return new PagedResult<UniversityDto>
        {
            Items = page.Items.Select(u => u.ToDto()).ToList(),
            PageNumber = page.PageNumber,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
        };
    }

    public async Task<UniversityDto?> GetByIdAsync(int id)
    {
        var repository = _unitOfWork.Repository<University>();
        var university = await repository.FirstOrDefaultAsync(u => u.Id == id);
        return university?.ToDto();
    }

    public async Task<IReadOnlyList<UniversityDto>> GetAllActiveAsync()
    {
        var repository = _unitOfWork.Repository<University>();
        var universities = await repository.FindAsync(u => u.IsActive);
        return universities.OrderBy(u => u.Name).Select(u => u.ToDto()).ToList();
    }

    public async Task<ServiceResult<int>> CreateAsync(UniversityFormViewModel model)
    {
        var repository = _unitOfWork.Repository<University>();

        if (await repository.AnyAsync(u => u.Code == model.Code))
        {
            return ServiceResult<int>.Failure("A university with this code already exists.");
        }

        var university = new University
        {
            Name = model.Name.Trim(),
            Code = model.Code.Trim(),
            City = string.IsNullOrWhiteSpace(model.City) ? null : model.City.Trim(),
            IsActive = model.IsActive,
        };

        await repository.AddAsync(university);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult<int>.Success(university.Id);
    }

    public async Task<ServiceResult> UpdateAsync(UniversityFormViewModel model)
    {
        var repository = _unitOfWork.Repository<University>();
        var university = await repository.GetByIdAsync(model.Id);

        if (university is null)
        {
            return ServiceResult.Failure("University not found.");
        }

        if (await repository.AnyAsync(u => u.Code == model.Code && u.Id != model.Id))
        {
            return ServiceResult.Failure("A university with this code already exists.");
        }

        university.Name = model.Name.Trim();
        university.Code = model.Code.Trim();
        university.City = string.IsNullOrWhiteSpace(model.City) ? null : model.City.Trim();
        university.IsActive = model.IsActive;

        repository.Update(university);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var repository = _unitOfWork.Repository<University>();
        var university = await repository.GetByIdAsync(id);

        if (university is null)
        {
            return ServiceResult.Failure("University not found.");
        }

        if (await _unitOfWork.Repository<Subject>().AnyAsync(s => s.UniversityId == id))
        {
            return ServiceResult.Failure("This university cannot be deleted because it has subjects associated with it.");
        }

        repository.Remove(university);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Success();
    }
}
