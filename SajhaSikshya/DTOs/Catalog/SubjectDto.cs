namespace SajhaSikshya.DTOs.Catalog;

/// <summary>Presentation-safe projection of <see cref="Data.Entities.Catalog.Subject"/>.</summary>
public class SubjectDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Code { get; set; }

    public int AcademicLevelId { get; set; }

    public string AcademicLevelName { get; set; } = string.Empty;

    public int? UniversityId { get; set; }

    public string? UniversityName { get; set; }

    public bool IsActive { get; set; }
}
