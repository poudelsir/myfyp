using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SajhaSikshya.Constants;
using SajhaSikshya.Data.Constants;
using SajhaSikshya.Extensions;
using SajhaSikshya.Helpers;
using SajhaSikshya.Services.Interfaces.Marketplace;

namespace SajhaSikshya.Areas.Student.Controllers;

/// <summary>
/// A Student's saved-listings ("save for later") area: the "My Saved Listings" grid
/// and the Toggle action used by the save/heart button on every listing card and the
/// listing details page. <see cref="Roles.Student"/>-only — the base
/// <see cref="Authorize"/> requirement alone would let an unauthenticated request
/// through to an anonymous-friendly path; requiring the Student role specifically is
/// what makes an Admin's click (a different authenticated role) correctly fail rather
/// than silently succeed.
/// </summary>
[Area("Student")]
[Authorize(Roles = Roles.Student)]
public class SavedListingsController : Controller
{
    private const int PageSize = PaginationConstants.DefaultPageSize;

    private readonly ISavedListingService _savedListingService;
    private readonly IListingSearchService _listingSearchService;
    private readonly ICompareService _compareService;

    public SavedListingsController(
        ISavedListingService savedListingService,
        IListingSearchService listingSearchService,
        ICompareService compareService)
    {
        _savedListingService = savedListingService;
        _listingSearchService = listingSearchService;
        _compareService = compareService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int pageNumber = 1)
    {
        var userId = User.GetUserId()!;
        var page = await _listingSearchService.GetMySavedListingsAsync(userId, pageNumber, PageSize);

        // Every item on this page is, by definition, one of the user's own saves —
        // no batch-check needed the way marketplace grids need one. Compare membership
        // still needs a lookup — this controller is Student-only, always authenticated,
        // so it's always the database path, never session.
        var compareIds = await _compareService.GetCompareListingIdsAsync(userId);
        foreach (var item in page.Items)
        {
            item.IsSaved = true;
            item.IsInCompare = compareIds.Contains(item.Id);
        }

        return View(page);
    }

    /// <summary>
    /// Saves or un-saves <paramref name="listingId"/> depending on its current state,
    /// then redirects back to wherever the button was clicked from
    /// (<paramref name="returnUrl"/>) — the save button appears on many different pages
    /// (homepage, filtered search, listing details, seller storefronts, this page's own
    /// grid), so redirecting to a fixed page would lose whatever the user was looking at.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int listingId, string? returnUrl)
    {
        var userId = User.GetUserId()!;
        var result = await _savedListingService.ToggleSaveAsync(userId, listingId);

        TempData[result.Succeeded ? AlertHelper.SuccessKey : AlertHelper.ErrorKey] =
            result.Succeeded
                ? (result.Data ? "Saved for later." : "Removed from saved listings.")
                : result.Errors.FirstOrDefault();

        return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Index));
    }
}
