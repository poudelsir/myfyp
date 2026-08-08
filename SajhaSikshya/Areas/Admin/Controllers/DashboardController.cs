using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SajhaSikshya.Data.Constants;
using SajhaSikshya.Services.Interfaces.Dashboard;
using SajhaSikshya.ViewModels.Admin.Shared;

namespace SajhaSikshya.Areas.Admin.Controllers;

/// <summary>
/// Administrator landing dashboard — the broad operational overview (Users, Listings,
/// Orders, Revenue, Donations, Verification, Reviews, Chat, AI usage, recent activity)
/// gathered by <see cref="IDashboardQueryService.GetAdminDashboardAsync"/>. Distinct
/// from <c>Areas/Admin/Insights</c>, which stays the deep AI-narrated analytics page.
/// </summary>
[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class DashboardController : Controller
{
    private readonly IDashboardQueryService _dashboardQueryService;

    public DashboardController(IDashboardQueryService dashboardQueryService)
    {
        _dashboardQueryService = dashboardQueryService;
    }

    public async Task<IActionResult> Index()
    {
        var model = new AdminDashboardViewModel
        {
            Stats = await _dashboardQueryService.GetAdminDashboardAsync(),
        };

        return View(model);
    }
}
