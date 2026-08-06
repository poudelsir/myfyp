using Microsoft.AspNetCore.Identity;
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
}
