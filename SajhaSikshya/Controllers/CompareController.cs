using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SajhaSikshya.Constants;
using SajhaSikshya.Extensions;
using SajhaSikshya.Helpers;
using SajhaSikshya.Services.Interfaces.Marketplace;

namespace SajhaSikshya.Controllers;

/// <summary>
/// Public-facing listing comparison (Milestone 4.3). Guests compare entirely via
/// session (<see cref="SessionExtensions"/>); authenticated users (Student or Admin —
/// nothing here is role-restricted, unlike Saved Listings, since comparing doesn't
/// mutate anything the way saving/selling does) persist via
/// <see cref="ICompareService"/>. Every action branches on
/// <c>User.Identity.IsAuthenticated</c> rather than on a specific role.
/// </summary>
[AllowAnonymous]
[Route("compare")]
public class CompareController : Controller
{
    private readonly ICompareService _compareService;
    private readonly IListingSearchService _listingSearchService;

    public CompareController(ICompareService compareService, IListingSearchService listingSearchService)
    {
        _compareService = compareService;
        _listingSearchService = listingSearchService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var ids = await GetCurrentCompareIdsAsync();
        var items = await _listingSearchService.GetComparisonAsync(ids);
        return View(items);
    }

    [HttpPost("add/{listingId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int listingId, string? returnUrl)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var result = await _compareService.AddAsync(User.GetUserId()!, listingId);
            TempData[result.Succeeded ? AlertHelper.SuccessKey : AlertHelper.ErrorKey] =
                result.Succeeded ? "Added to comparison." : result.Errors.FirstOrDefault();
        }
        else
        {
            var ids = HttpContext.Session.GetCompareListingIds().ToList();

            if (ids.Contains(listingId))
            {
                TempData[AlertHelper.ErrorKey] = "This listing is already in your comparison.";
            }
            else if (ids.Count >= SearchConstants.MaximumCompareCount)
            {
                TempData[AlertHelper.ErrorKey] = $"You can compare up to {SearchConstants.MaximumCompareCount} listings at a time. Remove one to add another.";
            }
            else if (!await _listingSearchService.IsListingActiveAsync(listingId))
            {
                TempData[AlertHelper.ErrorKey] = "This listing is not available to compare.";
            }
            else
            {
                ids.Add(listingId);
                HttpContext.Session.SetCompareListingIds(ids);
                TempData[AlertHelper.SuccessKey] = "Added to comparison.";
            }
        }

        return RedirectBack(returnUrl);
    }

    [HttpPost("remove/{listingId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int listingId, string? returnUrl)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            await _compareService.RemoveAsync(User.GetUserId()!, listingId);
        }
        else
        {
            var ids = HttpContext.Session.GetCompareListingIds().ToList();
            ids.Remove(listingId);
            HttpContext.Session.SetCompareListingIds(ids);
        }

        TempData[AlertHelper.SuccessKey] = "Removed from comparison.";
        return RedirectBack(returnUrl);
    }

    [HttpPost("clear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Clear(string? returnUrl)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            await _compareService.ClearAsync(User.GetUserId()!);
        }
        else
        {
            HttpContext.Session.ClearCompareListingIds();
        }

        TempData[AlertHelper.SuccessKey] = "Comparison cleared.";
        return RedirectBack(returnUrl);
    }

    private async Task<IReadOnlyList<int>> GetCurrentCompareIdsAsync()
    {
        return User.Identity?.IsAuthenticated == true
            ? await _compareService.GetCompareListingIdsAsync(User.GetUserId()!)
            : HttpContext.Session.GetCompareListingIds();
    }

    private IActionResult RedirectBack(string? returnUrl)
    {
        return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Index));
    }
}
