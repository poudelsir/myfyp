namespace SajhaSikshya.DTOs.Catalog;

/// <summary>A top-level category ("department") plus its listing count, for the Home page's featured-department tiles.</summary>
public class CategoryWithCountDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? IconName { get; set; }

    public string? Description { get; set; }

    /// <summary>Count of Active listings tagged with this department or any of its subcategories.</summary>
    public int ListingCount { get; set; }
}
