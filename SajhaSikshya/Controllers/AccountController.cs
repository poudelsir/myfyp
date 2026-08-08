using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using SajhaSikshya.Data.Constants;
using SajhaSikshya.Data.Entities;
using SajhaSikshya.Services.Interfaces;
using SajhaSikshya.ViewModels.Account;

namespace SajhaSikshya.Controllers;

/// <summary>
/// Handles login, registration, logout, and the forgot/reset password flow. All
/// authentication logic (including Identity's token generation/validation) lives in
/// <see cref="IAuthService"/> — this controller only validates input, calls the
/// service, decides where to redirect, and — for password reset — sends the email via
/// <see cref="IEmailService"/>. That three-step shape (Controller → Identity →
/// Email Service) is deliberate: Identity owns the security-sensitive token work,
/// this controller owns nothing riskier than composing a link and a redirect.
/// </summary>
public class AccountController : Controller
{
    private readonly IAuthService _authService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IAuthService authService, IEmailService emailService, ILogger<AccountController> logger)
    {
        _authService = authService;
        _emailService = emailService;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _authService.LoginAsync(model);

        if (result.Succeeded)
        {
            return LocalRedirectOrHome(model.ReturnUrl);
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "This account has been locked due to multiple failed attempts. Try again later.");
        }
        else if (result.IsNotAllowed)
        {
            ModelState.AddModelError(string.Empty, "This account is not allowed to sign in. Contact support if this seems wrong.");
        }
        else
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
        }

        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (result, user) = await _authService.RegisterStudentAsync(model);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        _logger.LogInformation("New student account registered: {Email}", user!.Email);

        TempData[Helpers.AlertHelper.SuccessKey] = "Account created successfully. Please sign in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        return View(new ForgotPasswordViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var tokenResult = await _authService.GeneratePasswordResetTokenAsync(model.Email);
        if (tokenResult is not null)
        {
            var (user, token) = tokenResult.Value;

            // Base64Url-encode the raw Identity token before it travels through a URL —
            // the standard Microsoft Identity pattern, since a raw token can contain
            // characters (+, /, =) that don't round-trip safely through a query string.
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var resetLink = Url.Action(nameof(ResetPassword), "Account", new { email = user.Email, token = encodedToken }, Request.Scheme)!;

            // Dev/ops visibility: EmailService itself logs-and-skips when SMTP isn't
            // configured (see its own remarks), so this is the only way to retrieve a
            // working reset link in an environment without real SMTP. Server-side log
            // only — never shown to the client.
            _logger.LogInformation("Password reset link generated for {Email}: {ResetLink}", user.Email, resetLink);

            await _emailService.SendEmailAsync(user.Email!, "Reset your SajhaSikshya password", BuildResetPasswordEmailHtml(user, resetLink));
        }

        // Identical response whether or not the email matched an account — never
        // reveal which emails are registered (prevents account enumeration).
        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPasswordConfirmation()
    {
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string? email = null, string? token = null)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
        {
            // No usable link parameters at all — nothing this form could do, so bounce
            // to Login rather than rendering a reset form that can only ever fail.
            return RedirectToAction(nameof(Login));
        }

        return View(new ResetPasswordViewModel { Email = email, Token = token });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        string decodedToken;
        try
        {
            decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Token));
        }
        catch (FormatException)
        {
            // A tampered/truncated token isn't even valid Base64Url — same generic
            // error as an expired or otherwise-rejected token, no distinction leaked.
            ModelState.AddModelError(string.Empty, "This password reset link is invalid or has expired.");
            return View(model);
        }

        var result = await _authService.ResetPasswordAsync(model.Email, decodedToken, model.NewPassword);
        if (result.Succeeded)
        {
            _logger.LogInformation("Password reset completed for {Email}.", model.Email);
            TempData[Helpers.AlertHelper.SuccessKey] = "Your password has been reset. Please sign in with your new password.";
            return RedirectToAction(nameof(Login));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    private IActionResult LocalRedirectOrHome(string? returnUrl)
    {
        return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// A small, functional (not Phase-12-styled) transactional email body — inline
    /// styles only, since email clients don't load external stylesheets.
    /// </summary>
    private static string BuildResetPasswordEmailHtml(ApplicationUser user, string resetLink)
    {
        var firstName = System.Net.WebUtility.HtmlEncode(user.FirstName);
        var encodedLink = System.Net.WebUtility.HtmlEncode(resetLink);

        return $"""
            <div style="font-family: Arial, sans-serif; max-width: 480px; margin: 0 auto; color: #1f2937;">
                <h2 style="color: #2563eb;">Reset your password</h2>
                <p>Hi {firstName},</p>
                <p>We received a request to reset the password for your SajhaSikshya account. Click the button below to choose a new password:</p>
                <p style="text-align: center; margin: 32px 0;">
                    <a href="{encodedLink}" style="background-color: #2563eb; color: #ffffff; padding: 12px 24px; border-radius: 8px; text-decoration: none; font-weight: bold; display: inline-block;">Reset Password</a>
                </p>
                <p>This link will expire in {SajhaSikshya.Constants.SecurityConstants.PasswordResetTokenLifespanHours} hours and can only be used once.</p>
                <p>If you didn't request a password reset, you can safely ignore this email — your password will not be changed.</p>
                <p style="color: #6b7280; font-size: 0.85rem; margin-top: 32px;">SajhaSikshya</p>
            </div>
            """;
    }
}
