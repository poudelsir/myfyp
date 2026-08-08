using SajhaSikshya.Data.Entities.Catalog;
using SajhaSikshya.DTOs.Catalog;

namespace SajhaSikshya.Mappings.Catalog;

public static class SubjectMappings
{
    /// <summary>Callers must Include(s => s.AcademicLevel) and Include(s => s.University) first.</summary>
    public static SubjectDto ToDto(this Subject subject)
    {
        return new SubjectDto
        {
            Id = subject.Id,
            Name = subject.Name,
            Code = subject.Code,
            AcademicLevelId = subject.AcademicLevelId,
            AcademicLevelName = subject.AcademicLevel?.Name ?? string.Empty,
            UniversityId = subject.UniversityId,
            UniversityName = subject.University?.Name,
            IsActive = subject.IsActive,
        };
    }
}
