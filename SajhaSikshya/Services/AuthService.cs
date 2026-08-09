using Microsoft.AspNetCore.Identity;
using SajhaSikshya.Data.Constants;
using SajhaSikshya.Data.Entities;
using SajhaSikshya.Services.Interfaces;
using SajhaSikshya.ViewModels.Account;

namespace SajhaSikshya.Services;

/// <summary>
/// Implements the account authentication/registration workflow described by
/// <see cref="IAuthService"/> on top of Identity's <see cref="UserManager{TUser}"/>
/// and <see cref="SignInManager{TUser}"/>.
/// </summary>
public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    public async Task<SignInResult> LoginAsync(LoginViewModel model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            // Report a generic failure so login doesn't leak which emails are registered.
            return SignInResult.Failed;
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login attempt for deactivated account {Email}.", model.Email);
            return SignInResult.NotAllowed;
        }

        var result = await _signInManager.PasswordSignInAsync(
            user,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            user.LastLoginAtUtc = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
        }

        return result;
    }

    public async Task<(IdentityResult Result, ApplicationUser? User)> RegisterStudentAsync(RegisterViewModel model)
    {
        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            IsActive = true,
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            return (result, null);
        }

        await _userManager.AddToRoleAsync(user, Roles.Student);
        return (result, user);
    }

    public async Task LogoutAsync()
    {
        await _signInManager.SignOutAsync();
    }

    public async Task<(ApplicationUser User, string Token)?> GeneratePasswordResetTokenAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null || !user.IsActive)
        {
            return null;
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        return (user, token);
    }

    public async Task<IdentityResult> ResetPasswordAsync(string email, string token, string newPassword)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            // Same generic failure Identity itself returns for a bad/expired token —
            // never a distinct "no such account" error, so this path can't be used to
            // enumerate registered emails either.
            return IdentityResult.Failed(new IdentityError
            {
                Code = "InvalidToken",
                Description = "This password reset link is invalid or has expired.",
            });
        }

        return await _userManager.ResetPasswordAsync(user, token, newPassword);
    }

    public async Task<IdentityResult> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return IdentityResult.Failed(new IdentityError { Code = "UserNotFound", Description = "Account not found." });
        }

        return await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
    }
}
