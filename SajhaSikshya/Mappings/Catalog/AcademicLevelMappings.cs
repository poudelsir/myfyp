using SajhaSikshya.Data.Entities.Catalog;
using SajhaSikshya.DTOs.Catalog;

namespace SajhaSikshya.Mappings.Catalog;

public static class AcademicLevelMappings
{
    /// <summary>Callers must Include(l => l.Subjects) for <see cref="AcademicLevelDto.SubjectCount"/> to be accurate.</summary>
    public static AcademicLevelDto ToDto(this AcademicLevel level)
    {
        return new AcademicLevelDto
        {
            Id = level.Id,
            Name = level.Name,
            Code = level.Code,
            DisplayOrder = level.DisplayOrder,
            IsActive = level.IsActive,
            SubjectCount = level.Subjects?.Count ?? 0,
        };
    }
}
