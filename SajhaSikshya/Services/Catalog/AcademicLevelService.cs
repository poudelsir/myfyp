using Microsoft.EntityFrameworkCore;
using SajhaSikshya.Data.Entities.Catalog;
using SajhaSikshya.DTOs;
using SajhaSikshya.DTOs.Catalog;
using SajhaSikshya.Mappings.Catalog;
using SajhaSikshya.Repositories.Interfaces;
using SajhaSikshya.Services.Interfaces.Catalog;
using SajhaSikshya.ViewModels.Admin.Catalog;

namespace SajhaSikshya.Services.Catalog;

public class AcademicLevelService : IAcademicLevelService
{
    private readonly IUnitOfWork _unitOfWork;

    public AcademicLevelService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<AcademicLevelDto>> GetPagedAsync(string? searchTerm, int pageNumber, int pageSize)
    {
        var repository = _unitOfWork.Repository<AcademicLevel>();

        var page = await repository.GetPagedAsync(
            pageNumber,
            pageSize,
            filter: string.IsNullOrWhiteSpace(searchTerm)
                ? null
                : l => l.Name.Contains(searchTerm) || l.Code.Contains(searchTerm),
            orderBy: q => q.OrderBy(l => l.DisplayOrder).ThenBy(l => l.Name),
            include: q => q.Include(l => l.Subjects));

        return new PagedResult<AcademicLevelDto>
        {
            Items = page.Items.Select(l => l.ToDto()).ToList(),
            PageNumber = page.PageNumber,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
        };
    }

    public async Task<AcademicLevelDto?> GetByIdAsync(int id)
    {
        var repository = _unitOfWork.Repository<AcademicLevel>();
        var level = await repository.FirstOrDefaultAsync(l => l.Id == id);
        return level?.ToDto();
    }

    public async Task<IReadOnlyList<AcademicLevelDto>> GetAllActiveAsync()
    {
        var repository = _unitOfWork.Repository<AcademicLevel>();
        var levels = await repository.FindAsync(l => l.IsActive);
        return levels.OrderBy(l => l.DisplayOrder).ThenBy(l => l.Name).Select(l => l.ToDto()).ToList();
    }

    public async Task<ServiceResult<int>> CreateAsync(AcademicLevelFormViewModel model)
    {
        var repository = _unitOfWork.Repository<AcademicLevel>();

        if (await repository.AnyAsync(l => l.Code == model.Code))
        {
            return ServiceResult<int>.Failure("An academic level with this code already exists.");
        }

        var level = new AcademicLevel
        {
            Name = model.Name.Trim(),
            Code = model.Code.Trim(),
            DisplayOrder = model.DisplayOrder,
            IsActive = model.IsActive,
        };

        await repository.AddAsync(level);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult<int>.Success(level.Id);
    }

    public async Task<ServiceResult> UpdateAsync(AcademicLevelFormViewModel model)
    {
        var repository = _unitOfWork.Repository<AcademicLevel>();
        var level = await repository.GetByIdAsync(model.Id);

        if (level is null)
        {
            return ServiceResult.Failure("Academic level not found.");
        }

        if (await repository.AnyAsync(l => l.Code == model.Code && l.Id != model.Id))
        {
            return ServiceResult.Failure("An academic level with this code already exists.");
        }

        level.Name = model.Name.Trim();
        level.Code = model.Code.Trim();
        level.DisplayOrder = model.DisplayOrder;
        level.IsActive = model.IsActive;

        repository.Update(level);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var repository = _unitOfWork.Repository<AcademicLevel>();
        var level = await repository.GetByIdAsync(id);

        if (level is null)
        {
            return ServiceResult.Failure("Academic level not found.");
        }

        if (await _unitOfWork.Repository<Subject>().AnyAsync(s => s.AcademicLevelId == id))
        {
            return ServiceResult.Failure("This academic level cannot be deleted because it has subjects associated with it.");
        }

        repository.Remove(level);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Success();
    }
}
