using Microsoft.EntityFrameworkCore;
using SajhaSikshya.Data.Entities.Catalog;
using SajhaSikshya.Data.Entities.Marketplace;
using SajhaSikshya.DTOs;
using SajhaSikshya.DTOs.Catalog;
using SajhaSikshya.Mappings.Catalog;
using SajhaSikshya.Repositories.Interfaces;
using SajhaSikshya.Services.Interfaces.Catalog;
using SajhaSikshya.ViewModels.Admin.Catalog;

namespace SajhaSikshya.Services.Catalog;

public class SubjectService : ISubjectService
{
    private readonly IUnitOfWork _unitOfWork;

    public SubjectService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<SubjectDto>> GetPagedAsync(string? searchTerm, int pageNumber, int pageSize)
    {
        var repository = _unitOfWork.Repository<Subject>();

        var page = await repository.GetPagedAsync(
            pageNumber,
            pageSize,
            filter: string.IsNullOrWhiteSpace(searchTerm)
                ? null
                : s => s.Name.Contains(searchTerm) || (s.Code != null && s.Code.Contains(searchTerm)),
            orderBy: q => q.OrderBy(s => s.Name),
            include: q => q.Include(s => s.AcademicLevel).Include(s => s.University));

        return new PagedResult<SubjectDto>
        {
            Items = page.Items.Select(s => s.ToDto()).ToList(),
            PageNumber = page.PageNumber,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
        };
    }

    public async Task<SubjectDto?> GetByIdAsync(int id)
    {
        var repository = _unitOfWork.Repository<Subject>();
        // FirstOrDefaultAsync doesn't eager-load navigations, so the related
        // AcademicLevel/University are attached manually for the mapper below.
        var subject = await repository.FirstOrDefaultAsync(s => s.Id == id);
        if (subject is null)
        {
            return null;
        }

        subject.AcademicLevel = (await _unitOfWork.Repository<AcademicLevel>().GetByIdAsync(subject.AcademicLevelId))!;
        if (subject.UniversityId.HasValue)
        {
            subject.University = await _unitOfWork.Repository<University>().GetByIdAsync(subject.UniversityId.Value);
        }

        return subject.ToDto();
    }

    public async Task<IReadOnlyList<SubjectDto>> GetAllActiveAsync()
    {
        var repository = _unitOfWork.Repository<Subject>();
        var subjects = await repository.FindAsync(s => s.IsActive);
        return subjects.OrderBy(s => s.Name).Select(s => s.ToDto()).ToList();
    }

    public async Task<ServiceResult<int>> CreateAsync(SubjectFormViewModel model)
    {
        var validationError = await ValidateReferencesAsync(model.AcademicLevelId, model.UniversityId);
        if (validationError is not null)
        {
            return ServiceResult<int>.Failure(validationError);
        }

        var subject = new Subject
        {
            Name = model.Name.Trim(),
            Code = string.IsNullOrWhiteSpace(model.Code) ? null : model.Code.Trim(),
            AcademicLevelId = model.AcademicLevelId,
            UniversityId = model.UniversityId,
            IsActive = model.IsActive,
        };

        var repository = _unitOfWork.Repository<Subject>();
        await repository.AddAsync(subject);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult<int>.Success(subject.Id);
    }

    public async Task<ServiceResult> UpdateAsync(SubjectFormViewModel model)
    {
        var repository = _unitOfWork.Repository<Subject>();
        var subject = await repository.GetByIdAsync(model.Id);

        if (subject is null)
        {
            return ServiceResult.Failure("Subject not found.");
        }

        var validationError = await ValidateReferencesAsync(model.AcademicLevelId, model.UniversityId);
        if (validationError is not null)
        {
            return ServiceResult.Failure(validationError);
        }

        subject.Name = model.Name.Trim();
        subject.Code = string.IsNullOrWhiteSpace(model.Code) ? null : model.Code.Trim();
        subject.AcademicLevelId = model.AcademicLevelId;
        subject.UniversityId = model.UniversityId;
        subject.IsActive = model.IsActive;

        repository.Update(subject);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var repository = _unitOfWork.Repository<Subject>();
        var subject = await repository.GetByIdAsync(id);

        if (subject is null)
        {
            return ServiceResult.Failure("Subject not found.");
        }

        if (await _unitOfWork.Repository<Listing>().AnyAsync(l => l.SubjectId == id))
        {
            return ServiceResult.Failure("This subject cannot be deleted because it has listings associated with it.");
        }

        repository.Remove(subject);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Success();
    }

    private async Task<string?> ValidateReferencesAsync(int academicLevelId, int? universityId)
    {
        if (!await _unitOfWork.Repository<AcademicLevel>().AnyAsync(l => l.Id == academicLevelId))
        {
            return "The selected academic level does not exist.";
        }

        if (universityId.HasValue && !await _unitOfWork.Repository<University>().AnyAsync(u => u.Id == universityId.Value))
        {
            return "The selected university does not exist.";
        }

        return null;
    }
}
