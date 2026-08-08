using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SajhaSikshya.Constants;
using SajhaSikshya.Data.Constants;
using SajhaSikshya.Data.Enums;
using SajhaSikshya.Extensions;
using SajhaSikshya.Helpers;
using SajhaSikshya.Services.Interfaces.Orders;
using SajhaSikshya.ViewModels.Admin.Orders;

namespace SajhaSikshya.Areas.Admin.Controllers;

/// <summary>
/// Admin oversight of every order in the system — search, filter, statistics, and
/// dispute resolution via <see cref="IOrderService.AdminCancelOrderAsync"/> (the same
/// <c>CancelInternalAsync</c> mechanics <c>Areas/Student/Controllers/OrdersController</c>'s
/// Reject/Cancel already exercise, just with no ownership check). Reads go through
/// <see cref="IOrderQueryService.GetAdminOrdersAsync"/>/<see cref="IOrderQueryService.GetOrderDetailsAsync"/>,
/// which are already unrestricted by owner — the "any order" access rule here needs
/// no controller-side check beyond the class-level <see cref="Roles.Admin"/> requirement,
/// unlike the Student controller's per-order buyer-or-seller check. Mirrors
/// <c>Admin/VerificationsController</c>'s shape exactly.
/// </summary>
[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class OrdersController : Controller
{
    private const int PageSize = PaginationConstants.DefaultPageSize;

    private readonly IOrderService _orderService;
    private readonly IOrderQueryService _orderQueryService;

    public OrdersController(IOrderService orderService, IOrderQueryService orderQueryService)
    {
        _orderService = orderService;
        _orderQueryService = orderQueryService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm, OrderStatus? status, int pageNumber = 1)
    {
        var model = new AdminOrderListViewModel
        {
            Page = await _orderQueryService.GetAdminOrdersAsync(searchTerm, status, pageNumber, PageSize),
            Statistics = await _orderQueryService.GetStatisticsAsync(),
            SearchTerm = searchTerm,
            Status = status,
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var order = await _orderQueryService.GetOrderDetailsAsync(id);
        if (order is null)
        {
            return NotFound();
        }

        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string reason)
    {
        var result = await _orderService.AdminCancelOrderAsync(id, User.GetUserId()!, reason);
        TempData[result.Succeeded ? AlertHelper.SuccessKey : AlertHelper.ErrorKey] =
            result.Succeeded ? "Order cancelled." : result.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Details), new { id });
    }
}
