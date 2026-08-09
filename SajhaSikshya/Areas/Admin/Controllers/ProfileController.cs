using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SajhaSikshya.Constants;
using SajhaSikshya.Data.Constants;
using SajhaSikshya.Data.Entities;
using SajhaSikshya.Helpers;
using SajhaSikshya.Services.Interfaces;
using SajhaSikshya.ViewModels.Account;
using SajhaSikshya.ViewModels.Admin.Profile;
using SajhaSikshya.ViewModels.Student.Profile;

namespace SajhaSikshya.Areas.Admin.Controllers;

/// <summary>
/// "My Profile" for the signed-in Administrator — the same private, owner-only shape
/// as <see cref="Areas.Student.Controllers.ProfileController"/> (Personal Information +
/// Security, reusing the exact same <see cref="PersonalInfoViewModel"/>/
/// <see cref="ChangePasswordViewModel"/> and the same <see cref="IImageStorageService"/>
/// photo pipeline), minus the seller/verification section that doesn't apply to an
/// Admin account. Two separate controllers rather than a shared base class, matching
/// how every other Admin/Student pair in this codebase (Listings, Dashboard, ...) is
/// already organized as parallel, not inherited, area controllers.
/// </summary>
[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class ProfileController : Controller
{
    private readonly IAuthService _authService;
    private readonly IImageStorageService _imageStorageService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public ProfileController(
        IAuthService authService,
        IImageStorageService imageStorageService,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _authService = authService;
        _imageStorageService = imageStorageService;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound();
        }

        return View(BuildViewModel(user));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePersonalInfo([Bind(Prefix = "PersonalInfo")] PersonalInfoViewModel model, string? returnUrl)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(nameof(Index), BuildViewModel(user, personalInfoOverride: model));
        }

        if (model.ProfilePhoto is not null)
        {
            var saveResult = await _imageStorageService.SaveAsync(
                model.ProfilePhoto,
                ProfileConstants.PhotoStorageSubfolder,
                ProfileConstants.AllowedPhotoExtensions,
                ProfileConstants.MaximumPhotoSizeMB);

            if (!saveResult.Succeeded)
            {
                ModelState.AddModelError(nameof(PersonalInfoViewModel.ProfilePhoto), saveResult.Errors.FirstOrDefault() ?? "The photo could not be uploaded.");
                return View(nameof(Index), BuildViewModel(user, personalInfoOverride: model));
            }

            var oldPhotoPath = user.ProfilePicturePath;
            user.ProfilePicturePath = saveResult.Data;
            if (!string.IsNullOrEmpty(oldPhotoPath))
            {
                await _imageStorageService.DeleteAsync(oldPhotoPath);
            }
        }

        user.FirstName = model.FirstName.Trim();
        user.LastName = model.LastName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();
        user.Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim();
        user.Institution = string.IsNullOrWhiteSpace(model.Institution) ? null : model.Institution.Trim();
        user.Bio = string.IsNullOrWhiteSpace(model.Bio) ? null : model.Bio.Trim();

        var updateResult = await _userManager.UpdateAsync(user);
        if (updateResult.Succeeded)
        {
            // Same claims-staleness fix as the Student profile page — see that
            // controller's identical comment.
            await _signInManager.RefreshSignInAsync(user);
        }

        TempData[updateResult.Succeeded ? AlertHelper.SuccessKey : AlertHelper.ErrorKey] =
            updateResult.Succeeded ? "Personal information updated." : updateResult.Errors.FirstOrDefault()?.Description;

        return RedirectBack(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword([Bind(Prefix = "ChangePassword")] ChangePasswordViewModel model, string? returnUrl)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            var vm = BuildViewModel(user);
            vm.ChangePassword = model;
            return View(nameof(Index), vm);
        }

        var result = await _authService.ChangePasswordAsync(user.Id, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            var vm = BuildViewModel(user);
            vm.ChangePassword = model;
            return View(nameof(Index), vm);
        }

        TempData[AlertHelper.SuccessKey] = "Password changed successfully.";
        return RedirectBack(returnUrl);
    }

    private IActionResult RedirectBack(string? returnUrl)
    {
        return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Index));
    }

    private static AdminProfileIndexViewModel BuildViewModel(ApplicationUser user, PersonalInfoViewModel? personalInfoOverride = null)
    {
        return new AdminProfileIndexViewModel
        {
            PersonalInfo = personalInfoOverride ?? new PersonalInfoViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                Institution = user.Institution,
                Bio = user.Bio,
                Email = user.Email ?? string.Empty,
                ProfilePicturePath = user.ProfilePicturePath,
            },
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            IsActive = user.IsActive,
            MemberSinceUtc = user.CreatedAtUtc,
            LastLoginAtUtc = user.LastLoginAtUtc,
        };
    }
}
