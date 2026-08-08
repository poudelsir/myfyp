using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SajhaSikshya.Constants;
using SajhaSikshya.Data.Entities;
using SajhaSikshya.Data.Enums;
using SajhaSikshya.Extensions;
using SajhaSikshya.Helpers;
using SajhaSikshya.Services.Interfaces.Reviews;
using SajhaSikshya.ViewModels.Reviews;

namespace SajhaSikshya.Controllers;

/// <summary>
/// Public review browsing — "Seller Reviews" and "Buyer Reviews" for a given user are
/// content any visitor can read (same reasoning as <c>MarketplaceController</c>), so
/// this lives at the root rather than under an Area, mirroring
/// <see cref="NotificationsController"/> and <c>VerificationImagesController</c> as a
/// resource reachable identically from both Student and Admin contexts — except here
/// the resource is public rather than owner-only. Reporting a review requires an
/// account, so only that single action is <see cref="AuthorizeAttribute"/>-gated.
/// The route shape (<c>/reviews/user/{userId}</c>) is load-bearing: it's exactly what
/// <c>ReviewService.ReviewLink</c> builds into every "new review received" notification.
/// </summary>
[Route("reviews")]
public class ReviewsController : Controller
{
    private const int PageSize = PaginationConstants.DefaultPageSize;

    private readonly IReviewQueryService _reviewQueryService;
    private readonly IReviewService _reviewService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReviewsController(IReviewQueryService reviewQueryService, IReviewService reviewService, UserManager<ApplicationUser> userManager)
    {
        _reviewQueryService = reviewQueryService;
        _reviewService = reviewService;
        _userManager = userManager;
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> ForUser(string userId, ReviewType type = ReviewType.BuyerToSeller, int? rating = null, int pageNumber = 1)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound();
        }

        var reputation = await _reviewQueryService.GetReputationAsync(userId);
        var model = new UserReviewsViewModel
        {
            UserId = userId,
            UserName = user.FullName,
            ReviewType = type,
            RatingFilter = rating,
            Reputation = reputation,
            Page = await _reviewQueryService.GetReviewsForUserAsync(userId, type, rating, pageNumber, PageSize),
        };

        return View(model);
    }

    [Authorize]
    [HttpPost("{id:int}/report")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Report(int id, string? returnUrl)
    {
        var result = await _reviewService.ReportAsync(id, User.GetUserId()!);
        TempData[result.Succeeded ? AlertHelper.SuccessKey : AlertHelper.ErrorKey] =
            result.Succeeded ? "Thanks — this review has been reported for moderator review." : result.Errors.FirstOrDefault();

        return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction("Index", "Home");
    }
}
