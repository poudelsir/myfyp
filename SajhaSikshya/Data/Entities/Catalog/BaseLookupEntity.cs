using System.ComponentModel.DataAnnotations;

namespace SajhaSikshya.Data.Entities.Catalog;

/// <summary>
/// Shared shape for the catalog's reference/lookup entities (<see cref="University"/>,
/// <see cref="AcademicLevel"/>, <see cref="Subject"/>, <see cref="Category"/>): every
/// one of them has a required display name and an admin-togglable active flag.
/// Fields that aren't universal (Code vs Slug, DisplayOrder) stay on the individual
/// entities rather than being forced in here.
/// </summary>
public abstract class BaseLookupEntity : BaseEntity
{
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Whether this entry is currently selectable when creating new content.</summary>
    public bool IsActive { get; set; } = true;
}
