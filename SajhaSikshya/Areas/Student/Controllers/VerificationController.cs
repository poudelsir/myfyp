using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SajhaSikshya.Constants;
using SajhaSikshya.Data.Constants;
using SajhaSikshya.Data.Entities;
using SajhaSikshya.Data.Enums;
using SajhaSikshya.Extensions;
using SajhaSikshya.Helpers;
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
    private readonly UserManager<ApplicationUser> _userManager;

    public VerificationController(
        IVerificationService verificationService,
        IVerificationQueryService verificationQueryService,
        UserManager<ApplicationUser> userManager)
    {
        _verificationService = verificationService;
        _verificationQueryService = verificationQueryService;
        _userManager = userManager;
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
        await PopulateAccountInfoAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(VerificationSubmissionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateAccountInfoAsync(model);
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

            await PopulateAccountInfoAsync(model);
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

        // Resubmission makes sense after a rejection (fix and try again) or from an
        // already-Verified seller updating their approved application (re-verification —
        // this new Pending row becomes the "current" row the moment it's inserted, so the
        // seller loses Seller Dashboard/Create Listing access until it's reviewed again,
        // the same as any other Pending request; no separate status handling needed since
        // IsUserVerifiedAsync/VerifiedStudentPolicy already read the latest row live).
        // Anything else (no history, already Pending) belongs on the dashboard instead.
        if (current is null || (current.Status != VerificationStatus.Rejected && current.Status != VerificationStatus.Verified))
        {
            return RedirectToAction(nameof(Index));
        }

        var model = new VerificationSubmissionViewModel();
        await PopulateAccountInfoAsync(model);
        ViewData["PreviousRejectionReason"] = current.RejectionReason;
        ViewData["WasVerified"] = current.Status == VerificationStatus.Verified;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Resubmit(VerificationSubmissionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateAccountInfoAsync(model);
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

            await PopulateAccountInfoAsync(model);
            return View(model);
        }

        TempData[AlertHelper.SuccessKey] = "Verification resubmitted! We'll review it shortly.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Fills the form's read-only account-info display — never bound from the submitted form, so it must be repopulated every time the form is (re)displayed, including after a failed POST.</summary>
    private async Task PopulateAccountInfoAsync(VerificationSubmissionViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return;
        }

        model.AccountFullName = user.FullName;
        model.AccountEmail = user.Email ?? string.Empty;
        model.AccountPhoneNumber = user.PhoneNumber;
    }
}
