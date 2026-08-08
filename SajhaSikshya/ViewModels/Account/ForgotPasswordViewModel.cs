using System.ComponentModel.DataAnnotations;

namespace SajhaSikshya.ViewModels.Account;

/// <summary>
/// Data submitted by the "Forgot Password" form. The controller must never use this
/// to reveal whether <see cref="Email"/> belongs to a real account — validation here
/// only checks the input is a well-formed email address, nothing more.
/// </summary>
public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
}
