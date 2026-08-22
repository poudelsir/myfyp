using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SajhaSikshya.Constants;
using SajhaSikshya.Data.Constants;
using SajhaSikshya.Data.Enums;
using SajhaSikshya.Extensions;
using SajhaSikshya.Helpers;
using SajhaSikshya.Services.Interfaces.Orders;
using SajhaSikshya.Services.Interfaces.Reviews;
using SajhaSikshya.ViewModels.Reviews;

namespace SajhaSikshya.Areas.Student.Controllers;

/// <summary>
/// A Student's own review activity — writing, editing, and deleting reviews they
/// authored, plus "My Reviews" (what they've written). Reading reviews ABOUT someone
/// else lives in the root-level <c>Controllers.ReviewsController</c> instead, since
/// that's public content any visitor (not just Students) can browse — the same
/// area-split reasoning Chat/Orders already use for "my own stuff" vs "public/shared".
/// <see cref="IReviewService"/> is the actual source of truth for every business rule
/// here (completed-order-only, one review per direction, edit window); this controller
/// only translates HTTP into those calls.
/// </summary>
[Area("Student")]
[Authorize(Roles = Roles.Student)]
public class ReviewsController : Controller
{
    private const int PageSize = PaginationConstants.DefaultPageSize;

    private readonly IReviewService _reviewService;
    private readonly IReviewQueryService _reviewQueryService;
    private readonly IOrderQueryService _orderQueryService;

    public ReviewsController(IReviewService reviewService, IReviewQueryService reviewQueryService, IOrderQueryService orderQueryService)
    {
        _reviewService = reviewService;
        _reviewQueryService = reviewQueryService;
        _orderQueryService = orderQueryService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int pageNumber = 1)
    {
        var model = new MyReviewsViewModel
        {
            Page = await _reviewQueryService.GetMyReviewsAsync(User.GetUserId()!, pageNumber, PageSize),
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Write(int orderId)
    {
        var userId = User.GetUserId()!;
        var order = await _orderQueryService.GetOrderDetailsAsync(orderId);
        if (order is null || (order.BuyerId != userId && order.SellerId != userId))
        {
            return NotFound();
        }

        if (order.Status != OrderStatus.Completed)
        {
            TempData[AlertHelper.ErrorKey] = "Only completed orders can be reviewed.";
            return RedirectToAction("Details", "Orders", new { id = orderId });
        }

        var reviewType = userId == order.BuyerId ? ReviewType.BuyerToSeller : ReviewType.SellerToBuyer;
        var existing = await _reviewQueryService.GetReviewForOrderAsync(orderId, reviewType);
        if (existing is not null)
        {
            return RedirectToAction(nameof(Edit), new { reviewId = existing.Id });
        }

        var model = new WriteReviewViewModel
        {
            OrderId = orderId,
            ListingTitle = order.ListingTitle,
            RevieweeName = userId == order.BuyerId ? order.SellerName : order.BuyerName,
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("write-actions")]
    public async Task<IActionResult> Write(WriteReviewViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _reviewService.CreateAsync(model.OrderId, User.GetUserId()!, model.Rating, model.Title, model.Comment);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Errors.FirstOrDefault() ?? "Could not submit your review.");
            return View(model);
        }

        TempData[AlertHelper.SuccessKey] = "Thank you — your review has been posted.";
        return RedirectToAction("Details", "Orders", new { id = model.OrderId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int reviewId)
    {
        var review = await _reviewQueryService.GetByIdAsync(reviewId);
        if (review is null || review.ReviewerId != User.GetUserId())
        {
            return NotFound();
        }

        var model = new WriteReviewViewModel
        {
            OrderId = review.OrderId,
            ReviewId = review.Id,
            ListingTitle = review.ListingTitle,
            RevieweeName = review.RevieweeName,
            Rating = review.Rating,
            Title = review.Title,
            Comment = review.Comment,
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(WriteReviewViewModel model)
    {
        if (model.ReviewId is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _reviewService.UpdateAsync(model.ReviewId.Value, User.GetUserId()!, model.Rating, model.Title, model.Comment);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Errors.FirstOrDefault() ?? "Could not update your review.");
            return View(model);
        }

        TempData[AlertHelper.SuccessKey] = "Review updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int reviewId)
    {
        var result = await _reviewService.DeleteAsync(reviewId, User.GetUserId()!);
        TempData[result.Succeeded ? AlertHelper.SuccessKey : AlertHelper.ErrorKey] =
            result.Succeeded ? "Review deleted." : result.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Index));
    }
}
