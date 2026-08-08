using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SajhaSikshya.ViewModels.Admin.Catalog;

/// <summary>Bound by both the Create and Edit forms for <c>CategoriesController</c>.</summary>
public class CategoryFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Category name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")]
    [Display(Name = "Category Name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Left blank to auto-generate from <see cref="Name"/> (see <see cref="Utilities.SlugHelper"/>).</summary>
    [StringLength(120)]
    [Display(Name = "URL Slug (optional)")]
    public string? Slug { get; set; }

    [Display(Name = "Parent Category (optional)")]
    public int? ParentCategoryId { get; set; }

    [Range(0, 999, ErrorMessage = "Display order must be between 0 and 999.")]
    [Display(Name = "Display Order")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public IEnumerable<SelectListItem> ParentCategoryOptions { get; set; } = Array.Empty<SelectListItem>();
}
