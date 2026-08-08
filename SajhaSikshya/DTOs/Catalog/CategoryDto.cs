namespace SajhaSikshya.DTOs.Catalog;

/// <summary>Presentation-safe projection of <see cref="Data.Entities.Catalog.Category"/>.</summary>
public class CategoryDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public int? ParentCategoryId { get; set; }

    public string? ParentCategoryName { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public int SubcategoryCount { get; set; }
}
