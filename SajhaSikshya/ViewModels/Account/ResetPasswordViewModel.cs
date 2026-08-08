using System.ComponentModel.DataAnnotations;

namespace SajhaSikshya.ViewModels.Account;

/// <summary>
/// Data submitted by the "Reset Password" form. <see cref="Email"/> and
/// <see cref="Token"/> round-trip through the reset link as hidden fields — the
/// controller never trusts them at face value, it re-validates both against
/// Identity's own token provider via <see cref="Services.Interfaces.IAuthService.ResetPasswordAsync"/>.
/// </summary>
public class ResetPasswordViewModel
{
    [Required]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The Base64Url-encoded password reset token from the link, still encoded at this
    /// point — the controller decodes it back to Identity's raw token before validating.
    /// </summary>
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your new password.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm New Password")]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
