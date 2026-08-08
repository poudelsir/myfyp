using SajhaSikshya.Data.Entities.Catalog;
using SajhaSikshya.DTOs.Catalog;

namespace SajhaSikshya.Mappings.Catalog;

public static class UniversityMappings
{
    /// <summary>
    /// Maps to <see cref="UniversityDto"/>. <see cref="UniversityDto.SubjectCount"/> reads
    /// off the already-loaded <see cref="University.Subjects"/> collection — callers must
    /// Include(u => u.Subjects) or the count will silently be 0.
    /// </summary>
    public static UniversityDto ToDto(this University university)
    {
        return new UniversityDto
        {
            Id = university.Id,
            Name = university.Name,
            Code = university.Code,
            City = university.City,
            IsActive = university.IsActive,
            SubjectCount = university.Subjects?.Count ?? 0,
        };
    }
}
