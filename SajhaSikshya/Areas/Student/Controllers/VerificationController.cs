using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SajhaSikshya.Constants;
using SajhaSikshya.Data.Constants;
using SajhaSikshya.Data.Enums;
using SajhaSikshya.Extensions;
using SajhaSikshya.Helpers;
using SajhaSikshya.Services.Interfaces.Catalog;
using SajhaSikshya.Services.Interfaces.Verification;
using SajhaSikshya.ViewModels.Verification;

namespace SajhaSikshya.Areas.Student.Controllers;

/// <summary>
/// Student-facing side of Phase 5 (Student Verification): the status/history
/// dashboard and the submit/resubmit forms. Mutations go through
/// <see cref="IVerificationService"/>; reads go through
/// <see cref="IVerificationQueryService"/>. The admin review side (queue, approve/
/// reject/request-resubmission) is a separate controller added in the next Phase 5
/// sub-step.
/// </summary>
[Area("Student")]
[Authorize(Roles = Roles.Student)]
public class VerificationController : Controller
{
    private const int HistoryPageSize = PaginationConstants.DefaultPageSize;

    private readonly IVerificationService _verificationService;
    private readonly IVerificationQueryService _verificationQueryService;
    private readonly IUniversityService _universityService;

    public VerificationController(
        IVerificationService verificationService,
        IVerificationQueryService verificationQueryService,
        IUniversityService universityService)
    {
        _verificationService = verificationService;
        _verificationQueryService = verificationQueryService;
        _universityService = universityService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int pageNumber = 1)
    {
        var userId = User.GetUserId()!;

        var model = new VerificationDashboardViewModel
        {
            Current = await _verificationQueryService.GetCurrentStatusAsync(userId),
            History = await _verificationQueryService.GetHistoryAsync(userId, pageNumber, HistoryPageSize),
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Submit()
    {
        var userId = User.GetUserId()!;
        var current = await _verificationQueryService.GetCurrentStatusAsync(userId);

        // Nothing to submit if a request is already Pending or already Verified —
        // send them back to the dashboard, which explains why.
        if (current is not null && current.Status != VerificationStatus.Rejected)
        {
            return RedirectToAction(nameof(Index));
        }

        var model = new VerificationSubmissionViewModel();
        await PopulateUniversitiesAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(VerificationSubmissionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateUniversitiesAsync(model);
            return View(model);
        }

        var userId = User.GetUserId()!;
        var result = await _verificationService.CreateAsync(userId, model);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            await PopulateUniversitiesAsync(model);
            return View(model);
        }

        TempData[AlertHelper.SuccessKey] = "Verification submitted! We'll review it shortly.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Resubmit()
    {
        var userId = User.GetUserId()!;
        var current = await _verificationQueryService.GetCurrentStatusAsync(userId);

        // Resubmission only makes sense after a rejection — anything else (no history,
        // Pending, already Verified) belongs on the dashboard instead.
        if (current is null || current.Status != VerificationStatus.Rejected)
        {
            return RedirectToAction(nameof(Index));
        }

        var model = new VerificationSubmissionViewModel { UniversityId = current.UniversityId };
        await PopulateUniversitiesAsync(model);
        ViewData["PreviousRejectionReason"] = current.RejectionReason;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Resubmit(VerificationSubmissionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateUniversitiesAsync(model);
            return View(model);
        }

        var userId = User.GetUserId()!;
        var result = await _verificationService.ResubmitAsync(userId, model);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            await PopulateUniversitiesAsync(model);
            return View(model);
        }

        TempData[AlertHelper.SuccessKey] = "Verification resubmitted! We'll review it shortly.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateUniversitiesAsync(VerificationSubmissionViewModel model)
    {
        var universities = await _universityService.GetAllActiveAsync();
        model.UniversityOptions = universities
            .Select(u => new SelectListItem(u.Name, u.Id.ToString()) { Selected = u.Id == model.UniversityId })
            .ToList();
    }
}
