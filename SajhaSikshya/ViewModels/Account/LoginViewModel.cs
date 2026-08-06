using System.ComponentModel.DataAnnotations;

namespace SajhaSikshya.ViewModels.Account;

/// <summary>
/// Data submitted by the login form. Validation here provides the first line of
/// defense before the request ever reaches <see cref="Services.Interfaces.IAuthService"/>.
/// </summary>
public class LoginViewModel
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    /// <summary>
    /// Local URL to redirect to after a successful login (e.g. the page that
    /// triggered the [Authorize] challenge). Validated against open-redirect
    /// attacks in the controller via Url.IsLocalUrl before use.
    /// </summary>
    public string? ReturnUrl { get; set; }
}
