using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SajhaSikshya.Data.Constants;
using SajhaSikshya.Services.Interfaces;
using SajhaSikshya.ViewModels.Account;

namespace SajhaSikshya.Controllers;

/// <summary>
/// Handles login, registration and logout. All authentication logic lives in
/// <see cref="IAuthService"/> — this controller only validates input, calls the
/// service, and decides where to redirect based on the result.
/// </summary>
public class AccountController : Controller
{
    private readonly IAuthService _authService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IAuthService authService, ILogger<AccountController> logger)
    {
        _authService = authService;
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

    private IActionResult LocalRedirectOrHome(string? returnUrl)
    {
        return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction("Index", "Home");
    }
}
