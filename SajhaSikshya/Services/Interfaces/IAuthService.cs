using Microsoft.AspNetCore.Identity;
using SajhaSikshya.Data.Entities;
using SajhaSikshya.ViewModels.Account;

namespace SajhaSikshya.Services.Interfaces;

/// <summary>
/// Encapsulates all account authentication/registration business logic on top of
/// ASP.NET Core Identity's <c>UserManager</c>/<c>SignInManager</c>. Controllers call
/// this instead of talking to Identity managers directly, keeping controllers thin
/// and making the login/registration flow independently testable.
/// </summary>
public interface IAuthService
{
    Task<SignInResult> LoginAsync(LoginViewModel model);

    /// <summary>
    /// Registers a new Student account and returns the Identity result along with
    /// the created user (null on failure) so the controller can sign them in.
    /// </summary>
    Task<(IdentityResult Result, Data.Entities.ApplicationUser? User)> RegisterStudentAsync(RegisterViewModel model);

    Task LogoutAsync();

    /// <summary>
    /// Generates a raw Identity password reset token for <paramref name="email"/> via
    /// <c>UserManager.GeneratePasswordResetTokenAsync</c> — Identity's own token
    /// provider, never a hand-rolled one. Returns <c>null</c> when no matching, active
    /// account exists; the caller (<c>AccountController</c>) must use that only to
    /// decide whether to actually send an email, and must show the exact same generic
    /// confirmation either way so the response never reveals which emails are registered.
    /// </summary>
    Task<(ApplicationUser User, string Token)?> GeneratePasswordResetTokenAsync(string email);

    /// <summary>
    /// Validates <paramref name="token"/> (Identity's own single-use, expiring token —
    /// never persisted or re-derived by this application) against <paramref name="email"/>
    /// and, if valid, sets <paramref name="newPassword"/> via
    /// <c>UserManager.ResetPasswordAsync</c>. An unknown email fails with the same
    /// generic "invalid or expired" error Identity itself returns for a bad token, so
    /// this path leaks nothing about account existence either.
    /// </summary>
    Task<IdentityResult> ResetPasswordAsync(string email, string token, string newPassword);
}
