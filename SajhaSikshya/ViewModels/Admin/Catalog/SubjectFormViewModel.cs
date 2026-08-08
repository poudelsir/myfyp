using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SajhaSikshya.ViewModels.Admin.Catalog;

/// <summary>Bound by both the Create and Edit forms for <c>SubjectsController</c>.</summary>
public class SubjectFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Subject name is required.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 200 characters.")]
    [Display(Name = "Subject Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "Code must be 20 characters or fewer.")]
    [Display(Name = "Curriculum Code")]
    public string? Code { get; set; }

    [Required(ErrorMessage = "Please select an academic level.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select an academic level.")]
    [Display(Name = "Academic Level")]
    public int AcademicLevelId { get; set; }

    [Display(Name = "University (optional)")]
    public int? UniversityId { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public IEnumerable<SelectListItem> AcademicLevelOptions { get; set; } = Array.Empty<SelectListItem>();

    public IEnumerable<SelectListItem> UniversityOptions { get; set; } = Array.Empty<SelectListItem>();
}
