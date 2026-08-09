using SajhaSikshya.Data.Entities.Catalog;
using SajhaSikshya.DTOs.Catalog;

namespace SajhaSikshya.Mappings.Catalog;

public static class CategoryMappings
{
    /// <summary>Callers must Include(c => c.ParentCategory) and Include(c => c.Subcategories) first.</summary>
    public static CategoryDto ToDto(this Category category)
    {
        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            ParentCategoryId = category.ParentCategoryId,
            ParentCategoryName = category.ParentCategory?.Name,
            DisplayOrder = category.DisplayOrder,
            IsActive = category.IsActive,
            SubcategoryCount = category.Subcategories?.Count ?? 0,
            IconName = category.IconName,
            Description = category.Description,
        };
    }
}
