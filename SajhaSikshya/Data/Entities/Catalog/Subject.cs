using System.ComponentModel.DataAnnotations;

namespace SajhaSikshya.Data.Entities.Catalog;

/// <summary>
/// An academic subject/course (e.g. "Data Structures and Algorithms"). Always
/// belongs to an <see cref="AcademicLevel"/>; optionally scoped to a specific
/// <see cref="Catalog.University"/> when the curriculum is university-specific.
/// </summary>
public class Subject : BaseLookupEntity
{
    /// <summary>Optional curriculum code (e.g. "CSC301"). Not enforced unique — codes can repeat across universities.</summary>
    [StringLength(20)]
    public string? Code { get; set; }

    public int AcademicLevelId { get; set; }

    public AcademicLevel AcademicLevel { get; set; } = null!;

    public int? UniversityId { get; set; }

    public University? University { get; set; }
}
