using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SajhaSikshya.Data.Constants;

namespace SajhaSikshya.Areas.Student.Controllers;

/// <summary>
/// Student landing dashboard. Restricted to the Student role; course/marketplace
/// content is added in a later phase — this establishes the routed, authorized
/// shell they'll plug into.
/// </summary>
[Area("Student")]
[Authorize(Roles = Roles.Student)]
public class DashboardController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
