using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SajhaSikshya.Data.Constants;
using SajhaSikshya.Extensions;
using SajhaSikshya.Services.Interfaces.AI;
using SajhaSikshya.ViewModels.Admin.AI;

namespace SajhaSikshya.Areas.Admin.Controllers;

/// <summary>
/// Admin AI Insights dashboard — the fourth and final Phase 10 AI feature.
/// <see cref="Index"/> renders real statistics synchronously (fast, database-only,
/// no external dependency); <see cref="Summary"/> is a separate fetch()-driven
/// endpoint for the AI narrative layer on top, so a Gemini failure there can never
/// prevent the dashboard's actual numbers from displaying (Phase 10.4's "never break
/// the dashboard").
/// </summary>
[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class InsightsController : Controller
{
    private readonly IAdminInsightsService _adminInsightsService;

    public InsightsController(IAdminInsightsService adminInsightsService)
    {
        _adminInsightsService = adminInsightsService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var model = new AdminInsightsViewModel
        {
            Stats = await _adminInsightsService.GetStatsAsync(),
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Summary()
    {
        var result = await _adminInsightsService.GenerateSummaryAsync(User.GetUserId()!);
        if (!result.Succeeded || result.Data is null)
        {
            return BadRequest(result.Errors.FirstOrDefault() ?? "Could not generate AI insights right now.");
        }

        return Json(result.Data);
    }
}
