using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SajhaSikshya.Data.Constants;
using SajhaSikshya.Extensions;
using SajhaSikshya.Helpers;
using SajhaSikshya.Services.Interfaces.Notifications;
using SajhaSikshya.ViewModels.Admin.Notifications;

namespace SajhaSikshya.Areas.Admin.Controllers;

/// <summary>
/// Admin-only broadcast composer — System Announcements, Maintenance Notices, and
/// role-targeted notices (Phase 8.3). Fans out through
/// <see cref="INotificationService.CreateBroadcastAsync"/>, the one place broadcast
/// fan-out logic lives; this controller only collects the form and reports how many
/// users actually received it. No persistent "broadcast log" is kept — a broadcast is
/// just N individual <c>Notification</c> rows sharing the same <c>CreatedBy</c>,
/// title, and message, not a distinct tracked entity of its own (see
/// <see cref="INotificationService"/>'s remarks on why that would be unnecessary
/// complexity for what this milestone actually needs).
/// </summary>
[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class BroadcastNotificationsController : Controller
{
    private readonly INotificationService _notificationService;

    public BroadcastNotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public IActionResult Index() => View(new BroadcastNotificationViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(BroadcastNotificationViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _notificationService.CreateBroadcastAsync(
            model.NotificationType,
            model.Title,
            model.Message,
            model.Link,
            User.GetUserId()!,
            model.TargetRole);

        TempData[result.Succeeded ? AlertHelper.SuccessKey : AlertHelper.ErrorKey] = result.Succeeded
            ? $"Broadcast sent to {result.Data} user{(result.Data == 1 ? "" : "s")}."
            : result.Errors.FirstOrDefault();

        return RedirectToAction(nameof(Index));
    }
}
