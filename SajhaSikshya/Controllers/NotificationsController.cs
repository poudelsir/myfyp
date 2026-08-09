using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SajhaSikshya.Constants;
using SajhaSikshya.DTOs.Notifications;
using SajhaSikshya.Extensions;
using SajhaSikshya.Helpers;
using SajhaSikshya.Services.Interfaces.Notifications;
using SajhaSikshya.ViewModels.Notifications;

namespace SajhaSikshya.Controllers;

/// <summary>
/// A user's own notifications — root-level rather than Area-scoped because both the
/// Student area and the Admin area need to reach the exact same controller (a
/// Student's and an Administrator's notifications are equally private; see
/// <see cref="INotificationQueryService"/>'s remarks on "no admin sees everyone's"
/// variant), the same reasoning <c>VerificationImagesController</c> already
/// established for a resource both areas need identical access to. Every action is
/// scoped to the caller's own id — there is no id-based lookup anywhere here that
/// isn't immediately re-checked against <c>User.GetUserId()</c>, so a notification id
/// belonging to someone else simply fails with "not found," never 403.
/// </summary>
[Authorize]
[Route("notifications")]
public class NotificationsController : Controller
{
    private const int PageSize = PaginationConstants.DefaultPageSize;

    private readonly INotificationService _notificationService;
    private readonly INotificationQueryService _notificationQueryService;

    public NotificationsController(INotificationService notificationService, INotificationQueryService notificationQueryService)
    {
        _notificationService = notificationService;
        _notificationQueryService = notificationQueryService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int pageNumber = 1)
    {
        var userId = User.GetUserId()!;
        var model = new NotificationCenterViewModel
        {
            Page = await _notificationQueryService.GetHistoryAsync(userId, pageNumber, PageSize),
        };

        return View(model);
    }

    [HttpPost("{id:int}/mark-read")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id, string? returnUrl)
    {
        await _notificationService.MarkAsReadAsync(id, User.GetUserId()!);
        return RedirectBack(returnUrl);
    }

    [HttpPost("mark-all-read")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead(string? returnUrl)
    {
        await _notificationService.MarkAllAsReadAsync(User.GetUserId()!);
        return RedirectBack(returnUrl);
    }

    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? returnUrl)
    {
        await _notificationService.DeleteAsync(id, User.GetUserId()!);
        return RedirectBack(returnUrl);
    }

    [HttpGet("preferences")]
    public async Task<IActionResult> Preferences()
    {
        var preferences = await _notificationQueryService.GetPreferencesAsync(User.GetUserId()!);
        return View(preferences);
    }

    [HttpPost("preferences")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Preferences(NotificationPreferenceDto preferences, string? returnUrl)
    {
        await _notificationService.UpdatePreferencesAsync(User.GetUserId()!, preferences);
        TempData[AlertHelper.SuccessKey] = "Notification preferences saved.";
        return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Preferences));
    }

    private IActionResult RedirectBack(string? returnUrl)
    {
        return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Index));
    }
}
