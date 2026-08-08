namespace SajhaSikshya.DTOs.Catalog;

/// <summary>Presentation-safe projection of <see cref="Data.Entities.Catalog.University"/>.</summary>
public class UniversityDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string? City { get; set; }

    public bool IsActive { get; set; }

    /// <summary>Number of non-deleted subjects referencing this university; used to warn before delete.</summary>
    public int SubjectCount { get; set; }
}
