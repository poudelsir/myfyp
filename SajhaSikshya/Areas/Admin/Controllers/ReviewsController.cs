using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SajhaSikshya.Constants;
using SajhaSikshya.Data.Constants;
using SajhaSikshya.Data.Enums;
using SajhaSikshya.Extensions;
using SajhaSikshya.Helpers;
using SajhaSikshya.Services.Interfaces.Reviews;
using SajhaSikshya.ViewModels.Admin.Reviews;

namespace SajhaSikshya.Areas.Admin.Controllers;

/// <summary>
/// Admin review moderation queue. Every mutation calls the single
/// <see cref="IReviewService.ModerateAsync"/> (no ownership check — same reasoning as
/// <c>VerificationsController</c>), gated purely by the <see cref="Roles.Admin"/> role
/// requirement below. The route this controller lives at (<c>/Admin/Reviews</c>) is
/// load-bearing: it's exactly what <c>ReviewService.AdminReviewQueueLink</c> builds
/// into the "review reported" broadcast notification sent to admins.
/// </summary>
[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class ReviewsController : Controller
{
    private const int PageSize = PaginationConstants.DefaultPageSize;

    private readonly IReviewService _reviewService;
    private readonly IReviewQueryService _reviewQueryService;

    public ReviewsController(IReviewService reviewService, IReviewQueryService reviewQueryService)
    {
        _reviewService = reviewService;
        _reviewQueryService = reviewQueryService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(bool reportedOnly = true, int pageNumber = 1)
    {
        var model = new AdminReviewQueueViewModel
        {
            ReportedOnly = reportedOnly,
            Page = await _reviewQueryService.GetAdminQueueAsync(reportedOnly, pageNumber, PageSize),
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int id, string? reason, bool reportedOnly = true)
    {
        var result = await _reviewService.ModerateAsync(id, ReviewModerationAction.Remove, User.GetUserId()!, reason);
        TempData[result.Succeeded ? AlertHelper.SuccessKey : AlertHelper.ErrorKey] =
            result.Succeeded ? "Review removed." : result.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Index), new { reportedOnly });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, bool reportedOnly = true)
    {
        var result = await _reviewService.ModerateAsync(id, ReviewModerationAction.Restore, User.GetUserId()!);
        TempData[result.Succeeded ? AlertHelper.SuccessKey : AlertHelper.ErrorKey] =
            result.Succeeded ? "Review restored." : result.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Index), new { reportedOnly });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetReportCount(int id, bool reportedOnly = true)
    {
        var result = await _reviewService.ModerateAsync(id, ReviewModerationAction.ResetReportCount, User.GetUserId()!);
        TempData[result.Succeeded ? AlertHelper.SuccessKey : AlertHelper.ErrorKey] =
            result.Succeeded ? "Report count cleared." : result.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Index), new { reportedOnly });
    }
}
