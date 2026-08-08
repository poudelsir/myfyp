namespace SajhaSikshya.DTOs.Catalog;

/// <summary>Presentation-safe projection of <see cref="Data.Entities.Catalog.AcademicLevel"/>.</summary>
public class AcademicLevelDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public int SubjectCount { get; set; }
}
