using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SajhaSikshya.ViewModels.Student.Profile;

/// <summary>
/// Bound from the Profile page's "Personal Information" form. Deliberately excludes
/// Email — changing a primary email needs a re-confirmation flow this app doesn't
/// have yet, so it's shown read-only instead of collected here.
/// </summary>
public class PersonalInfoViewModel
{
    [Required(ErrorMessage = "Please enter your first name.")]
    [StringLength(100, MinimumLength = 2)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your last name.")]
    [StringLength(100, MinimumLength = 2)]
    public string LastName { get; set; } = string.Empty;

    [Phone]
    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [StringLength(300)]
    public string? Address { get; set; }

    [StringLength(150)]
    public string? Institution { get; set; }

    [StringLength(500)]
    public string? Bio { get; set; }

    /// <summary>Optional — leaving this empty keeps the existing photo.</summary>
    public IFormFile? ProfilePhoto { get; set; }

    /// <summary>Populated by the controller for read-only display — never bound from the submitted form.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Populated by the controller for read-only display — never bound from the submitted form.</summary>
    public string? ProfilePicturePath { get; set; }
}
