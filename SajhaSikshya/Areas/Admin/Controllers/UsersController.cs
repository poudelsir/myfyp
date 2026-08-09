using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SajhaSikshya.Constants;
using SajhaSikshya.Data.Constants;
using SajhaSikshya.Extensions;
using SajhaSikshya.Helpers;
using SajhaSikshya.Services.Interfaces.Users;
using SajhaSikshya.ViewModels.Admin.Users;

namespace SajhaSikshya.Areas.Admin.Controllers;

/// <summary>
/// Admin "Manage Users" module. Reads go through <see cref="IUserQueryService"/>;
/// the only mutation here is Suspend/Reactivate (<see cref="IUserManagementService"/>)
/// — Approve/Reject Seller and viewing verification documents are deliberately NOT
/// re-implemented here; the Details page links out to the existing, unmodified
/// <c>Areas/Admin/Controllers/VerificationsController</c> for those.
/// </summary>
[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class UsersController : Controller
{
    private const int PageSize = PaginationConstants.DefaultPageSize;

    private readonly IUserQueryService _userQueryService;
    private readonly IUserManagementService _userManagementService;

    public UsersController(IUserQueryService userQueryService, IUserManagementService userManagementService)
    {
        _userQueryService = userQueryService;
        _userManagementService = userManagementService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm, string? role, bool? isActive, int pageNumber = 1)
    {
        var page = await _userQueryService.GetUsersAsync(searchTerm, role, isActive, pageNumber, PageSize);
        return View(new AdminUserListViewModel { Page = page, SearchTerm = searchTerm, Role = role, IsActive = isActive });
    }

    [HttpGet]
    public async Task<IActionResult> Details(string id)
    {
        var user = await _userQueryService.GetUserDetailAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Suspend(string id)
    {
        var adminId = User.GetUserId()!;
        var result = await _userManagementService.SetActiveStatusAsync(id, isActive: false, adminId);

        TempData[result.Succeeded ? AlertHelper.SuccessKey : AlertHelper.ErrorKey] =
            result.Succeeded ? "User suspended." : result.Errors.FirstOrDefault();

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reactivate(string id)
    {
        var adminId = User.GetUserId()!;
        var result = await _userManagementService.SetActiveStatusAsync(id, isActive: true, adminId);

        TempData[result.Succeeded ? AlertHelper.SuccessKey : AlertHelper.ErrorKey] =
            result.Succeeded ? "User reactivated." : result.Errors.FirstOrDefault();

        return RedirectToAction(nameof(Details), new { id });
    }
}
