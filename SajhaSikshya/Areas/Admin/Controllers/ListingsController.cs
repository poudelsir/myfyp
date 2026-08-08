using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SajhaSikshya.Constants;
using SajhaSikshya.Data.Constants;
using SajhaSikshya.Data.Enums;
using SajhaSikshya.Extensions;
using SajhaSikshya.Helpers;
using SajhaSikshya.Services.Interfaces.Marketplace;
using SajhaSikshya.ViewModels.Admin.Marketplace;

namespace SajhaSikshya.Areas.Admin.Controllers;

/// <summary>
/// Admin listing moderation queue. Every mutation here calls the single, unrestricted
/// <see cref="IListingService.ModerateListingAsync"/> (no ownership check — that's the
/// whole point of this controller existing), gated purely by the
/// <see cref="Roles.Admin"/> role requirement below.
/// </summary>
[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class ListingsController : Controller
{
    private const int PageSize = PaginationConstants.DefaultPageSize;

    private readonly IListingService _listingService;
    private readonly IListingQueryService _listingQueryService;

    public ListingsController(IListingService listingService, IListingQueryService listingQueryService)
    {
        _listingService = listingService;
        _listingQueryService = listingQueryService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm, ListingStatus? status, int pageNumber = 1)
    {
        var page = await _listingQueryService.GetAllForAdminAsync(searchTerm, status, pageNumber, PageSize);
        return View(new AdminListingListViewModel { Page = page, SearchTerm = searchTerm, Status = status });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var listing = await _listingQueryService.GetForAdminAsync(id);
        if (listing is null)
        {
            return NotFound();
        }

        return View(listing);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var result = await _listingService.ModerateListingAsync(id, ModerationAction.Approve, User.GetUserId()!);
        TempData[result.Succeeded ? AlertHelper.SuccessKey : AlertHelper.ErrorKey] =
            result.Succeeded ? "Listing approved and is now live." : result.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? reason)
    {
        var result = await _listingService.ModerateListingAsync(id, ModerationAction.Reject, User.GetUserId()!, reason);
        TempData[result.Succeeded ? AlertHelper.SuccessKey : AlertHelper.ErrorKey] =
            result.Succeeded ? "Listing rejected." : result.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(int id)
    {
        var result = await _listingService.ModerateListingAsync(id, ModerationAction.Archive, User.GetUserId()!);
        TempData[result.Succeeded ? AlertHelper.SuccessKey : AlertHelper.ErrorKey] =
            result.Succeeded ? "Listing archived." : result.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id)
    {
        var result = await _listingService.ModerateListingAsync(id, ModerationAction.Restore, User.GetUserId()!);
        TempData[result.Succeeded ? AlertHelper.SuccessKey : AlertHelper.ErrorKey] =
            result.Succeeded ? "Listing restored and sent back for review." : result.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _listingService.ModerateListingAsync(id, ModerationAction.Delete, User.GetUserId()!);
        TempData[result.Succeeded ? AlertHelper.SuccessKey : AlertHelper.ErrorKey] =
            result.Succeeded ? "Listing deleted." : result.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Index));
    }
}
