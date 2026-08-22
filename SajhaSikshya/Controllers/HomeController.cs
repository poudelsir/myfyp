using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SajhaSikshya.ViewModels.Shared;

namespace SajhaSikshya.Controllers;

/// <summary>
/// The Home page — SajhaSikshya's landing page for every audience, Admin included, not
/// a pass-through redirect. It introduces the platform (hero, AI features, trust
/// section, featured content) the same way for a guest and a logged-in User/Seller/Admin;
/// the view itself adapts small pieces (hero CTA wording, "Become a Seller" vs
/// "Seller Dashboard") based on auth/verification state rather than the controller
/// branching to a different destination. An Admin is still a platform user first — they
/// reach their Admin Panel deliberately (profile dropdown → Admin Panel), the same way a
/// Student reaches Seller Center, rather than being forced there on every login.
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
        return View();
    }

    [AllowAnonymous]
    public IActionResult About()
    {
        return View();
    }

    [AllowAnonymous]
    public IActionResult HelpCenter()
    {
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
