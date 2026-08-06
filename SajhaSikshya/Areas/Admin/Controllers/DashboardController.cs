using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SajhaSikshya.Data.Constants;

namespace SajhaSikshya.Areas.Admin.Controllers;

/// <summary>
/// Administrator landing dashboard. Restricted to the Admin role; the marketplace
/// widgets (enrollment stats, revenue charts, etc.) are added in a later phase —
/// this establishes the routed, authorized shell they'll plug into.
/// </summary>
[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class DashboardController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
