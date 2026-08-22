using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SajhaSikshya.Data.Constants;
using SajhaSikshya.Data.Enums;
using SajhaSikshya.Extensions;
using SajhaSikshya.Helpers;
using SajhaSikshya.Services.Interfaces.Orders;
using SajhaSikshya.Services.Interfaces.Payments;

namespace SajhaSikshya.Areas.Student.Controllers;

/// <summary>
/// A simulated eSewa/Khalti checkout — there is no real merchant account behind this,
/// so <see cref="IPaymentGatewayService"/> stands in for one (see its remarks). The
/// checkout screen and the pay/fail actions exist so the *rest* of the payment flow
/// (order state, notifications, receipts) can be built and demonstrated against
/// something real-shaped, without needing production gateway credentials this project
/// doesn't have.
/// </summary>
[Area("Student")]
[Authorize(Roles = Roles.Student)]
public class PaymentsController : Controller
{
    private readonly IOrderQueryService _orderQueryService;
    private readonly IPaymentGatewayService _paymentGatewayService;

    public PaymentsController(IOrderQueryService orderQueryService, IPaymentGatewayService paymentGatewayService)
    {
        _orderQueryService = orderQueryService;
        _paymentGatewayService = paymentGatewayService;
    }

    [HttpGet]
    public async Task<IActionResult> Checkout(int orderId)
    {
        var order = await _orderQueryService.GetOrderDetailsAsync(orderId);
        if (order is null || order.BuyerId != User.GetUserId() || !IsPayableOnline(order.PaymentMethod, order.PaymentStatus))
        {
            return NotFound();
        }

        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("write-actions")]
    public async Task<IActionResult> Complete(int orderId)
    {
        var result = await _paymentGatewayService.SimulateSuccessAsync(orderId, User.GetUserId()!);
        if (!result.Succeeded)
        {
            TempData[AlertHelper.ErrorKey] = result.Errors.FirstOrDefault();
            return RedirectToAction(nameof(Checkout), new { orderId });
        }

        TempData[AlertHelper.SuccessKey] = "Payment simulated successfully — no real money moved.";
        return RedirectToAction("Details", "Orders", new { id = orderId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("write-actions")]
    public async Task<IActionResult> Fail(int orderId)
    {
        var result = await _paymentGatewayService.SimulateFailureAsync(orderId, User.GetUserId()!);
        if (!result.Succeeded)
        {
            TempData[AlertHelper.ErrorKey] = result.Errors.FirstOrDefault();
        }
        else
        {
            TempData[AlertHelper.ErrorKey] = "Simulated payment failed. You can retry or switch to Cash on Pickup.";
        }

        return RedirectToAction(nameof(Checkout), new { orderId });
    }

    private static bool IsPayableOnline(PaymentMethod method, PaymentStatus status) =>
        method is PaymentMethod.ESewa or PaymentMethod.Khalti
        && status is PaymentStatus.Pending or PaymentStatus.Failed;
}
