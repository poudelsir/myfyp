using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SajhaSikshya.Constants;
using SajhaSikshya.Data.Constants;
using SajhaSikshya.Data.Enums;
using SajhaSikshya.Extensions;
using SajhaSikshya.Helpers;
using SajhaSikshya.Services.Interfaces.Verification;
using SajhaSikshya.ViewModels.Admin.Verification;

namespace SajhaSikshya.Areas.Admin.Controllers;

/// <summary>
/// Admin verification review queue. Every mutation here calls the single
/// <see cref="IVerificationService.ReviewAsync"/> (no ownership check — that's the
/// whole point of this controller existing), gated purely by the
/// <see cref="Roles.Admin"/> role requirement below. Mirrors
/// <c>Admin/ListingsController</c>'s shape exactly.
/// </summary>
[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class VerificationsController : Controller
{
    private const int PageSize = PaginationConstants.DefaultPageSize;

    private readonly IVerificationService _verificationService;
    private readonly IVerificationQueryService _verificationQueryService;

    public VerificationsController(IVerificationService verificationService, IVerificationQueryService verificationQueryService)
    {
        _verificationService = verificationService;
        _verificationQueryService = verificationQueryService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm, VerificationStatus? status, int pageNumber = 1)
    {
        var page = await _verificationQueryService.GetQueueAsync(status, searchTerm, pageNumber, PageSize);
        return View(new AdminVerificationListViewModel { Page = page, SearchTerm = searchTerm, Status = status });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var verification = await _verificationQueryService.GetForReviewAsync(id);
        if (verification is null)
        {
            return NotFound();
        }

        var model = new AdminVerificationDetailViewModel
        {
            Verification = verification,
            History = await _verificationQueryService.GetHistoryAsync(verification.UserId, 1, PageSize),
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var result = await _verificationService.ReviewAsync(id, VerificationAction.Approve, User.GetUserId()!);
        TempData[result.Succeeded ? AlertHelper.SuccessKey : AlertHelper.ErrorKey] =
            result.Succeeded ? "Verification approved." : result.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string reason, string? adminNotes)
    {
        var result = await _verificationService.ReviewAsync(id, VerificationAction.Reject, User.GetUserId()!, reason, adminNotes);
        TempData[result.Succeeded ? AlertHelper.SuccessKey : AlertHelper.ErrorKey] =
            result.Succeeded ? "Verification rejected." : result.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestResubmission(int id, string reason, string? adminNotes)
    {
        var result = await _verificationService.ReviewAsync(id, VerificationAction.RequestResubmission, User.GetUserId()!, reason, adminNotes);
        TempData[result.Succeeded ? AlertHelper.SuccessKey : AlertHelper.ErrorKey] =
            result.Succeeded ? "Resubmission requested." : result.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Index));
    }
}
