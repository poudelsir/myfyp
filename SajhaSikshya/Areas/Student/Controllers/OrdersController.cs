using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SajhaSikshya.Authorization;
using SajhaSikshya.Constants;
using SajhaSikshya.Data.Constants;
using SajhaSikshya.Data.Enums;
using SajhaSikshya.Extensions;
using SajhaSikshya.Helpers;
using SajhaSikshya.Services.Interfaces.Orders;
using SajhaSikshya.Services.Interfaces.Reviews;
using SajhaSikshya.ViewModels.Orders;

namespace SajhaSikshya.Areas.Student.Controllers;

/// <summary>
/// A Student's own orders — both sides of the marketplace transaction live in one
/// controller because a Student can be a buyer on one order and a seller on
/// another; "My Orders" is one feature area with a Buying/Selling toggle, not two.
/// Mutations go through <see cref="IOrderService"/>; reads go through
/// <see cref="IOrderQueryService"/>. <see cref="Create"/> is the one action gated by
/// <see cref="AuthorizationPolicies.VerifiedStudent"/> — the first place that policy
/// (built in Phase 5, deliberately left unenforced) is actually applied. Every other
/// action here checks the caller is a party to the specific order (buyer or seller)
/// rather than any Student — the Admin area has its own Orders controller for the
/// "Admin may also access any order" half of that rule.
/// </summary>
[Area("Student")]
[Authorize(Roles = Roles.Student)]
public class OrdersController : Controller
{
    private const int PageSize = PaginationConstants.DefaultPageSize;

    private readonly IOrderService _orderService;
    private readonly IOrderQueryService _orderQueryService;
    private readonly IReviewQueryService _reviewQueryService;

    public OrdersController(IOrderService orderService, IOrderQueryService orderQueryService, IReviewQueryService reviewQueryService)
    {
        _orderService = orderService;
        _orderQueryService = orderQueryService;
        _reviewQueryService = reviewQueryService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string tab = "buying", OrderStatus? status = null, int pageNumber = 1)
    {
        var userId = User.GetUserId()!;
        var isSelling = string.Equals(tab, "selling", StringComparison.OrdinalIgnoreCase);

        var model = new MyOrdersViewModel
        {
            Tab = isSelling ? "selling" : "buying",
            Status = status,
            Page = isSelling
                ? await _orderQueryService.GetSellerOrdersAsync(userId, status, pageNumber, PageSize)
                : await _orderQueryService.GetBuyerOrdersAsync(userId, status, pageNumber, PageSize),
        };

        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.VerifiedStudent)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int listingId, string? returnUrl)
    {
        var buyerId = User.GetUserId()!;
        var result = await _orderService.CreateOrderAsync(buyerId, listingId);

        if (!result.Succeeded)
        {
            TempData[AlertHelper.ErrorKey] = result.Errors.FirstOrDefault();
            return RedirectBack(returnUrl);
        }

        TempData[AlertHelper.SuccessKey] = "Request sent! The seller will review it shortly.";
        return RedirectToAction(nameof(Details), new { id = result.Data });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var order = await _orderQueryService.GetOrderDetailsAsync(id);
        if (order is null || !IsPartyToOrder(order.BuyerId, order.SellerId))
        {
            return NotFound();
        }

        if (order.Status == OrderStatus.Completed)
        {
            var userId = User.GetUserId()!;
            var reviewType = userId == order.BuyerId ? ReviewType.BuyerToSeller : ReviewType.SellerToBuyer;
            ViewData["MyReviewForOrder"] = await _reviewQueryService.GetReviewForOrderAsync(id, reviewType);
        }

        return View(order);
    }

    [HttpGet]
    public async Task<IActionResult> Receipt(int id)
    {
        var order = await _orderQueryService.GetOrderDetailsAsync(id);
        if (order is null || !IsPartyToOrder(order.BuyerId, order.SellerId))
        {
            return NotFound();
        }

        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Accept(int id)
    {
        var result = await _orderService.AcceptOrderAsync(id, User.GetUserId()!);
        TempData[result.Succeeded ? AlertHelper.SuccessKey : AlertHelper.ErrorKey] =
            result.Succeeded ? "Order accepted." : result.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string reason)
    {
        var result = await _orderService.RejectOrderAsync(id, User.GetUserId()!, reason);
        TempData[result.Succeeded ? AlertHelper.SuccessKey : AlertHelper.ErrorKey] =
            result.Succeeded ? "Order rejected." : result.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkReady(int id, string? pickupNotes)
    {
        var result = await _orderService.MarkReadyForPickupAsync(id, User.GetUserId()!, pickupNotes);
        TempData[result.Succeeded ? AlertHelper.SuccessKey : AlertHelper.ErrorKey] =
            result.Succeeded ? "Marked ready for pickup." : result.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmPickup(int id)
    {
        var result = await _orderService.ConfirmPickupAsync(id, User.GetUserId()!);
        TempData[result.Succeeded ? AlertHelper.SuccessKey : AlertHelper.ErrorKey] =
            result.Succeeded ? "Pickup confirmed — order complete!" : result.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string reason)
    {
        var result = await _orderService.CancelOrderAsync(id, User.GetUserId()!, reason);
        TempData[result.Succeeded ? AlertHelper.SuccessKey : AlertHelper.ErrorKey] =
            result.Succeeded ? "Order cancelled." : result.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Details), new { id });
    }

    private bool IsPartyToOrder(string buyerId, string sellerId)
    {
        var userId = User.GetUserId();
        return userId == buyerId || userId == sellerId;
    }

    private IActionResult RedirectBack(string? returnUrl)
    {
        return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Index));
    }
}
