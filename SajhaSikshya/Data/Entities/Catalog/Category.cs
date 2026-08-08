using System.ComponentModel.DataAnnotations;

namespace SajhaSikshya.Data.Entities.Catalog;

/// <summary>
/// A content category (e.g. "Notes", "Past Papers"), optionally nested under a
/// parent category to form a two-level taxonomy. Foundational lookup entity that
/// future marketplace listings will be classified under.
/// </summary>
public class Category : BaseLookupEntity
{
    /// <summary>URL-safe identifier (e.g. "past-papers"). Unique among non-deleted categories.</summary>
    [Required]
    [StringLength(120)]
    public string Slug { get; set; } = string.Empty;

    public int? ParentCategoryId { get; set; }

    public Category? ParentCategory { get; set; }

    public ICollection<Category> Subcategories { get; set; } = new List<Category>();

    /// <summary>Manual sort order for UI listings.</summary>
    public int DisplayOrder { get; set; }
}
