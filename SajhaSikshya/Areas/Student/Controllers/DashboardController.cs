using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SajhaSikshya.Data.Constants;
using SajhaSikshya.Extensions;
using SajhaSikshya.Services.Interfaces.Reviews;
using SajhaSikshya.Services.Interfaces.Verification;

namespace SajhaSikshya.Areas.Student.Controllers;

/// <summary>
/// Student landing dashboard. Restricted to the Student role; course/marketplace
/// content is added in a later phase — this establishes the routed, authorized
/// shell they'll plug into. Verification status is the one piece of real data
/// wired in so far, shown as the "Verified Student" badge next to the greeting.
/// </summary>
[Area("Student")]
[Authorize(Roles = Roles.Student)]
public class DashboardController : Controller
{
    private readonly IVerificationQueryService _verificationQueryService;
    private readonly IReviewQueryService _reviewQueryService;

    public DashboardController(IVerificationQueryService verificationQueryService, IReviewQueryService reviewQueryService)
    {
        _verificationQueryService = verificationQueryService;
        _reviewQueryService = reviewQueryService;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.GetUserId()!;
        ViewData["IsVerified"] = await _verificationQueryService.IsUserVerifiedAsync(userId);
        ViewData["Reputation"] = await _reviewQueryService.GetReputationAsync(userId);
        return View();
    }
}
