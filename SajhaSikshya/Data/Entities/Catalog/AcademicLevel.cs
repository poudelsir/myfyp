using System.ComponentModel.DataAnnotations;

namespace SajhaSikshya.Data.Entities.Catalog;

/// <summary>
/// A level of study (e.g. "+2", "Bachelor's Degree", "Master's Degree") that
/// <see cref="Subject"/> records belong to. Foundational lookup entity.
/// </summary>
public class AcademicLevel : BaseLookupEntity
{
    /// <summary>Short, admin-chosen code (e.g. "BACHELOR"). Unique among non-deleted levels.</summary>
    [Required]
    [StringLength(20, MinimumLength = 2)]
    public string Code { get; set; } = string.Empty;

    /// <summary>Manual sort order for UI listings (levels have a natural progression, not alphabetical).</summary>
    public int DisplayOrder { get; set; }

    public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
}
