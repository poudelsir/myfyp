using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SajhaSikshya.Data.Constants;
using SajhaSikshya.Extensions;
using SajhaSikshya.Services.AI.Prompts;
using SajhaSikshya.Services.Interfaces.AI;
using SajhaSikshya.ViewModels.AI;

namespace SajhaSikshya.Controllers;

/// <summary>
/// The Marketplace Assistant chat page — root-level and reachable identically from both
/// the Student and Admin areas (same reasoning as <see cref="NotificationsController"/>
/// and <see cref="ReviewsController"/>: a resource scoped to the caller, not to a role).
/// Conversation history lives in <see cref="HttpContext.Session"/>
/// (<see cref="SessionExtensions"/>) rather than the database — it's short-lived by
/// design (Phase 10.3's "avoid unbounded context growth"), so there's nothing here for
/// <see cref="IMarketplaceAssistantService"/> to persist beyond one Gemini call.
/// </summary>
[Authorize]
[Route("assistant")]
public class AssistantController : Controller
{
    private readonly IMarketplaceAssistantService _assistantService;

    public AssistantController(IMarketplaceAssistantService assistantService)
    {
        _assistantService = assistantService;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        var model = new AssistantViewModel
        {
            History = HttpContext.Session.GetAssistantHistory(),
            SuggestedQuestions = MarketplaceAssistantPromptBuilder.SuggestedQuestions,
        };

        return View(model);
    }

    [HttpPost("ask")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ask(string question)
    {
        var role = User.IsInRole(Roles.Admin) ? Roles.Admin : Roles.Student;
        var history = HttpContext.Session.GetAssistantHistory();

        var result = await _assistantService.AskAsync(User.GetUserId()!, role, question, history);
        if (!result.Succeeded || result.Data is null)
        {
            return BadRequest(result.Errors.FirstOrDefault() ?? "Could not reach the assistant right now.");
        }

        HttpContext.Session.AppendAssistantExchange(question.Trim(), result.Data);

        return Json(new { reply = result.Data });
    }

    [HttpPost("reset")]
    [ValidateAntiForgeryToken]
    public IActionResult Reset()
    {
        HttpContext.Session.ClearAssistantHistory();
        return Ok();
    }
}
