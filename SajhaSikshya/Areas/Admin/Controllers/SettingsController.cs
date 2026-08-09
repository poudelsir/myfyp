using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SajhaSikshya.Data.Constants;
using SajhaSikshya.Data.Entities;
using SajhaSikshya.Services.Interfaces.Notifications;
using SajhaSikshya.ViewModels.Admin.Settings;

namespace SajhaSikshya.Areas.Admin.Controllers;

/// <summary>
/// Admin Settings — same tabbed-shell-over-existing-modules shape as
/// <see cref="Areas.Student.Controllers.SettingsController"/>, minus Privacy/Seller
/// (neither applies to an Administrator) and with a read-only System Preferences tab
/// instead. No new mutations of its own — Profile/Security post to
/// <see cref="ProfileController"/>, Notifications posts to the same root-level
/// <c>NotificationsController</c> every area shares.
/// </summary>
[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class SettingsController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly INotificationQueryService _notificationQueryService;

    public SettingsController(UserManager<ApplicationUser> userManager, INotificationQueryService notificationQueryService)
    {
        _userManager = userManager;
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

        var model = new AdminSettingsViewModel
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
            IsActive = user.IsActive,
        };

        return View(model);
    }
}
