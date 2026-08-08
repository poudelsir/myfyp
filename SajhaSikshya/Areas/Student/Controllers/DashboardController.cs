using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SajhaSikshya.Data.Constants;
using SajhaSikshya.Extensions;
using SajhaSikshya.Services.Interfaces.Dashboard;
using SajhaSikshya.ViewModels.Student.Dashboard;

namespace SajhaSikshya.Areas.Student.Controllers;

/// <summary>
/// Student landing dashboard — real listing/order/saved/compare/message/notification
/// stats, verification status, reputation, recommendations, and recent activity, all
/// gathered by <see cref="IDashboardQueryService.GetStudentDashboardAsync"/>.
/// </summary>
[Area("Student")]
[Authorize(Roles = Roles.Student)]
public class DashboardController : Controller
{
    private readonly IDashboardQueryService _dashboardQueryService;

    public DashboardController(IDashboardQueryService dashboardQueryService)
    {
        _dashboardQueryService = dashboardQueryService;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.GetUserId()!;
        var model = new StudentDashboardViewModel
        {
            Stats = await _dashboardQueryService.GetStudentDashboardAsync(userId),
        };

        return View(model);
    }
}
