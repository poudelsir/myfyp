using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SajhaSikshya.Constants;
using SajhaSikshya.Data.Constants;
using SajhaSikshya.Data.Entities;
using SajhaSikshya.Data.Enums;
using SajhaSikshya.Extensions;
using SajhaSikshya.Helpers;
using SajhaSikshya.Services.Interfaces;
using SajhaSikshya.Services.Interfaces.Verification;
using SajhaSikshya.ViewModels.Account;
using SajhaSikshya.ViewModels.Student.Profile;

namespace SajhaSikshya.Areas.Student.Controllers;

/// <summary>
/// "My Profile" — the private, owner-only profile page. Personal Information and
/// Security are genuinely owned here (backed by <see cref="ApplicationUser"/>); Seller
/// Information surfaces the verification module's approved data without duplicating
/// it (see <see cref="IVerificationQueryService.GetCurrentStatusAsync"/>) — the one
/// exception is Selling Categories, which is safe to edit in place through
/// <see cref="IVerificationService.UpdateSellingCategoriesAsync"/> since it's declared
/// moderation/insights-only metadata, not an identity claim. Changing anything else
/// about a seller application (Seller Type, Institution, documents) goes through the
/// existing Resubmit flow instead.
/// </summary>
[Area("Student")]
[Authorize(Roles = Roles.Student)]
public class ProfileController : Controller
{
    private readonly IVerificationQueryService _verificationQueryService;
    private readonly IVerificationService _verificationService;
    private readonly IAuthService _authService;
    private readonly IImageStorageService _imageStorageService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public ProfileController(
        IVerificationQueryService verificationQueryService,
        IVerificationService verificationService,
        IAuthService authService,
        IImageStorageService imageStorageService,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _verificationQueryService = verificationQueryService;
        _verificationService = verificationService;
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

        return View(await BuildViewModelAsync(user));
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
            return View(nameof(Index), await BuildViewModelAsync(user, personalInfoOverride: model));
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
                return View(nameof(Index), await BuildViewModelAsync(user, personalInfoOverride: model));
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
            // FirstName/LastName are embedded in the auth cookie's claims at sign-in
            // (ApplicationUserClaimsPrincipalFactory) and otherwise only refresh on next
            // login — without this, a name change wouldn't show up in the navbar/sidebar
            // until the user signs out and back in.
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
            var vm = await BuildViewModelAsync(user);
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

            var vm = await BuildViewModelAsync(user);
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSellingCategories(List<SellingCategory> categories)
    {
        var userId = User.GetUserId()!;
        var result = await _verificationService.UpdateSellingCategoriesAsync(userId, categories);

        TempData[result.Succeeded ? AlertHelper.SuccessKey : AlertHelper.ErrorKey] =
            result.Succeeded ? "Selling categories updated." : result.Errors.FirstOrDefault();

        return RedirectToAction(nameof(Index));
    }

    private async Task<ProfileIndexViewModel> BuildViewModelAsync(ApplicationUser user, PersonalInfoViewModel? personalInfoOverride = null)
    {
        var current = await _verificationQueryService.GetCurrentStatusAsync(user.Id);

        return new ProfileIndexViewModel
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
            Verification = current,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            MemberSinceUtc = user.CreatedAtUtc,
        };
    }
}
