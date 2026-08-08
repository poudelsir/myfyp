using System.ComponentModel.DataAnnotations;

namespace SajhaSikshya.ViewModels.Admin.Catalog;

/// <summary>Bound by both the Create and Edit forms for <c>UniversitiesController</c>.</summary>
public class UniversityFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "University name is required.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 200 characters.")]
    [Display(Name = "University Name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Code is required.")]
    [StringLength(20, MinimumLength = 2, ErrorMessage = "Code must be between 2 and 20 characters.")]
    [Display(Name = "Short Code")]
    public string Code { get; set; } = string.Empty;

    [StringLength(100)]
    public string? City { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}
