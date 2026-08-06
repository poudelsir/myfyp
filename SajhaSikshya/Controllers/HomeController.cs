using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SajhaSikshya.Data.Constants;
using SajhaSikshya.ViewModels.Shared;

namespace SajhaSikshya.Controllers;

/// <summary>
/// Public landing page and the application's generic error/status-code page.
/// Kept intentionally thin: routing authenticated users to their dashboard is the
/// only "logic" here, and even that is a simple role check.
/// </summary>
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return User.IsInRole(Roles.Admin)
                ? RedirectToAction("Index", "Dashboard", new { area = "Admin" })
                : RedirectToAction("Index", "Dashboard", new { area = "Student" });
        }

        return View();
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? statusCode = null)
    {
        var message = statusCode switch
        {
            404 => "The page you're looking for could not be found.",
            403 => "You don't have permission to access this resource.",
            _ => null,
        };

        if (statusCode.HasValue)
        {
            Response.StatusCode = statusCode.Value;
        }

        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            Message = message,
        });
    }
}
