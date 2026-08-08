using System.ComponentModel.DataAnnotations;

namespace SajhaSikshya.ViewModels.Admin.Catalog;

/// <summary>Bound by both the Create and Edit forms for <c>AcademicLevelsController</c>.</summary>
public class AcademicLevelFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Level name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")]
    [Display(Name = "Level Name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Code is required.")]
    [StringLength(20, MinimumLength = 2, ErrorMessage = "Code must be between 2 and 20 characters.")]
    [Display(Name = "Short Code")]
    public string Code { get; set; } = string.Empty;

    [Range(0, 999, ErrorMessage = "Display order must be between 0 and 999.")]
    [Display(Name = "Display Order")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}
