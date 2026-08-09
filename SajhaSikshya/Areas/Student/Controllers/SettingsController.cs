using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SajhaSikshya.Data.Constants;
using SajhaSikshya.Data.Entities;
using SajhaSikshya.Helpers;
using SajhaSikshya.Services.Interfaces.Notifications;
using SajhaSikshya.Services.Interfaces.Verification;
using SajhaSikshya.ViewModels.Student.Settings;

namespace SajhaSikshya.Areas.Student.Controllers;

/// <summary>
/// Settings — a tabbed shell (Profile/Security/Notifications/Privacy/Seller) over
/// existing modules; every tab's edit action posts to the module that already owns
/// that data (<see cref="ProfileController"/>, root-level <c>NotificationsController</c>,
/// <see cref="IVerificationService"/>'s existing flows). The only genuinely new mutation
/// here is <see cref="UpdatePrivacy"/> — everything else this controller does is reads
/// composed for display.
/// </summary>
[Area("Student")]
[Authorize(Roles = Roles.Student)]
public class SettingsController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IVerificationQueryService _verificationQueryService;
    private readonly INotificationQueryService _notificationQueryService;

    public SettingsController(
        UserManager<ApplicationUser> userManager,
        IVerificationQueryService verificationQueryService,
        INotificationQueryService notificationQueryService)
    {
        _userManager = userManager;
        _verificationQueryService = verificationQueryService;
        _notificationQueryService = notificationQueryService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound();
        }

        var model = new StudentSettingsViewModel
        {
            PersonalInfo = new()
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
            NotificationPreferences = await _notificationQueryService.GetPreferencesAsync(user.Id),
            IsPublicProfile = user.IsPublicProfile,
            Verification = await _verificationQueryService.GetCurrentStatusAsync(user.Id),
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePrivacy(bool isPublicProfile)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound();
        }

        user.IsPublicProfile = isPublicProfile;
        await _userManager.UpdateAsync(user);

        TempData[AlertHelper.SuccessKey] = "Privacy settings saved.";
        return RedirectToAction(nameof(Index));
    }
}
