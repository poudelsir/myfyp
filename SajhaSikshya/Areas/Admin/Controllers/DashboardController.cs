using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SajhaSikshya.Data.Constants;
using SajhaSikshya.Services.Interfaces.Marketplace;
using SajhaSikshya.Services.Interfaces.Reviews;
using SajhaSikshya.Services.Interfaces.Verification;
using SajhaSikshya.ViewModels.Admin.Shared;

namespace SajhaSikshya.Areas.Admin.Controllers;

/// <summary>Administrator landing dashboard — moderation-queue and verification-queue stats at a glance.</summary>
[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class DashboardController : Controller
{
    private readonly IListingQueryService _listingQueryService;
    private readonly IVerificationQueryService _verificationQueryService;
    private readonly IReviewQueryService _reviewQueryService;

    public DashboardController(IListingQueryService listingQueryService, IVerificationQueryService verificationQueryService, IReviewQueryService reviewQueryService)
    {
        _listingQueryService = listingQueryService;
        _verificationQueryService = verificationQueryService;
        _reviewQueryService = reviewQueryService;
    }

    public async Task<IActionResult> Index()
    {
        var reportedReviews = await _reviewQueryService.GetAdminQueueAsync(reportedOnly: true, pageNumber: 1, pageSize: 1);
        var model = new AdminDashboardViewModel
        {
            ListingStats = await _listingQueryService.GetModerationStatsAsync(),
            VerificationStats = await _verificationQueryService.GetStatisticsAsync(),
            ReportedReviewCount = reportedReviews.TotalCount,
        };

        return View(model);
    }
}
