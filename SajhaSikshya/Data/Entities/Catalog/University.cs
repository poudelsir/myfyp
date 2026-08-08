using System.ComponentModel.DataAnnotations;

namespace SajhaSikshya.Data.Entities.Catalog;

/// <summary>
/// An academic institution (e.g. Tribhuvan University) that <see cref="Subject"/>
/// records can optionally be scoped to. One of the foundational lookup entities
/// every future marketplace listing will be tagged against.
/// </summary>
public class University : BaseLookupEntity
{
    /// <summary>Short, admin-chosen code (e.g. "TU"). Unique among non-deleted universities.</summary>
    [Required]
    [StringLength(20, MinimumLength = 2)]
    public string Code { get; set; } = string.Empty;

    [StringLength(100)]
    public string? City { get; set; }

    public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
}
