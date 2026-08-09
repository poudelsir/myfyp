using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SajhaSikshya.Constants;
using SajhaSikshya.Data.Constants;
using SajhaSikshya.Data.Enums;
using SajhaSikshya.DTOs.Marketplace;
using SajhaSikshya.Extensions;
using SajhaSikshya.Services.Interfaces.Catalog;
using SajhaSikshya.Services.Interfaces.Marketplace;
using SajhaSikshya.Services.Interfaces.Orders;
using SajhaSikshya.Services.Interfaces.Reviews;
using SajhaSikshya.Services.Interfaces.Verification;
using SajhaSikshya.ViewModels.Marketplace;

namespace SajhaSikshya.Controllers;

/// <summary>
/// Public-facing marketplace: browsing/search, listing details, and seller storefronts.
/// Guests and Students both land here with identical read-only access — nothing in
/// this controller requires authentication. Search/filter/sort goes through
/// <see cref="IListingSearchService"/>; the fixed-shape reads (Featured/Recent rails,
/// listing details, seller storefronts) go through <see cref="IListingQueryService"/>;
/// the one mutation (view-count) goes through <see cref="IListingService"/>, gated by a
/// per-session "already viewed" check kept here rather than in the service, since
/// session state is an HTTP concern, not a business rule. Every DTO handed to a view
/// also gets its <c>IsSaved</c> flag stamped here (via <see cref="ISavedListingService"/>)
/// for authenticated requests — the query/search services have no ambient "current
/// user" to stamp it with themselves.
/// </summary>
[AllowAnonymous]
[Route("marketplace")]
public class MarketplaceController : Controller
{
    private const int PageSize = PaginationConstants.DefaultPageSize;
    private const int FeaturedCount = 8;
    private const int RecentCount = 8;
    private const int RelatedCount = 4;
    private const string ViewedListingSessionKeyPrefix = "viewed-listing-";

    private readonly IListingQueryService _listingQueryService;
    private readonly IListingSearchService _listingSearchService;
    private readonly IListingService _listingService;
    private readonly ISavedListingService _savedListingService;
    private readonly ICompareService _compareService;
    private readonly IVerificationQueryService _verificationQueryService;
    private readonly IReviewQueryService _reviewQueryService;
    private readonly IOrderQueryService _orderQueryService;
    private readonly ICategoryService _categoryService;
    private readonly IUniversityService _universityService;
    private readonly IAcademicLevelService _academicLevelService;
    private readonly ISubjectService _subjectService;

    public MarketplaceController(
        IListingQueryService listingQueryService,
        IListingSearchService listingSearchService,
        IListingService listingService,
        ISavedListingService savedListingService,
        ICompareService compareService,
        IVerificationQueryService verificationQueryService,
        IReviewQueryService reviewQueryService,
        IOrderQueryService orderQueryService,
        ICategoryService categoryService,
        IUniversityService universityService,
        IAcademicLevelService academicLevelService,
        ISubjectService subjectService)
    {
        _listingQueryService = listingQueryService;
        _listingSearchService = listingSearchService;
        _listingService = listingService;
        _savedListingService = savedListingService;
        _compareService = compareService;
        _verificationQueryService = verificationQueryService;
        _reviewQueryService = reviewQueryService;
        _orderQueryService = orderQueryService;
        _categoryService = categoryService;
        _universityService = universityService;
        _academicLevelService = academicLevelService;
        _subjectService = subjectService;
    }

    /// <summary>
    /// Stamps <c>IsSaved</c> across every listing referenced by <paramref name="collections"/>
    /// with a single batched lookup — a no-op for anonymous requests, since guests can
    /// never have anything saved. Called once per action with every DTO collection that
    /// action is about to render, rather than once per collection, so one page never
    /// issues more than one "which of these are saved" query.
    /// </summary>
    private async Task MarkSavedAsync(params IReadOnlyList<ListingSummaryDto>[] collections)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var allListings = collections.SelectMany(c => c).ToList();
        if (allListings.Count == 0)
        {
            return;
        }

        var userId = User.GetUserId()!;
        var savedIds = await _savedListingService.GetSavedListingIdsAsync(userId, allListings.Select(l => l.Id).Distinct());

        foreach (var listing in allListings)
        {
            listing.IsSaved = savedIds.Contains(listing.Id);
        }
    }

    /// <summary>
    /// Stamps <c>IsInCompare</c> across every listing referenced by <paramref name="collections"/>.
    /// Unlike <see cref="MarkSavedAsync"/>, this runs for guests too — comparison is
    /// available to everyone, just backed by session instead of the database — and the
    /// list is capped at <see cref="SearchConstants.MaximumCompareCount"/> (4), so
    /// checking membership in memory is cheap enough that no batched query is needed.
    /// </summary>
    private async Task MarkCompareAsync(params IReadOnlyList<ListingSummaryDto>[] collections)
    {
        var compareIds = await GetCurrentCompareIdsAsync();
        if (compareIds.Count == 0)
        {
            return;
        }

        foreach (var listing in collections.SelectMany(c => c))
        {
            listing.IsInCompare = compareIds.Contains(listing.Id);
        }
    }

    private async Task<IReadOnlyList<int>> GetCurrentCompareIdsAsync()
    {
        return User.Identity?.IsAuthenticated == true
            ? await _compareService.GetCompareListingIdsAsync(User.GetUserId()!)
            : HttpContext.Session.GetCompareListingIds();
    }

    /// <summary>
    /// Stamps <c>IsSellerVerified</c> across every listing referenced by
    /// <paramref name="collections"/> with a single batched lookup — about the seller,
    /// not the viewer, so (unlike <see cref="MarkSavedAsync"/>) this runs unconditionally
    /// for guests too, same as <see cref="MarkCompareAsync"/>.
    /// </summary>
    private async Task MarkSellerVerifiedAsync(params IReadOnlyList<ListingSummaryDto>[] collections)
    {
        var allListings = collections.SelectMany(c => c).ToList();
        if (allListings.Count == 0)
        {
            return;
        }

        var verifiedSellerIds = await _verificationQueryService.GetVerifiedUserIdsAsync(allListings.Select(l => l.SellerId).Distinct());

        foreach (var listing in allListings)
        {
            listing.IsSellerVerified = verifiedSellerIds.Contains(listing.SellerId);
        }
    }

    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] ListingSearchCriteria criteria)
    {
        var model = new MarketplaceHomeViewModel
        {
            Criteria = criteria,
            Browse = await _listingSearchService.SearchAsync(criteria, PageSize),
            BrowseCategories = await _categoryService.GetEligibleParentCategoriesAsync(excludeCategoryId: null),
            BrowseUniversities = await _universityService.GetAllActiveAsync(),
            BrowseAcademicLevels = await _academicLevelService.GetAllActiveAsync(),
            BrowseSubjects = await _subjectService.GetAllActiveAsync(),
        };

        // The homepage rails only make sense on the unfiltered first page; a filtered,
        // searched, or paged view just shows the grid under a "Browsing: X"/"Search
        // results" heading instead.
        if (criteria.PageNumber <= 1 && !model.IsFiltered)
        {
            model.Featured = await _listingQueryService.GetFeaturedListingsAsync(FeaturedCount);
            model.Recent = await _listingQueryService.GetRecentListingsAsync(RecentCount);
        }

        if (criteria.CategoryId.HasValue)
        {
            model.CategoryName = model.BrowseCategories.FirstOrDefault(c => c.Id == criteria.CategoryId)?.Name
                ?? model.Browse.Items.FirstOrDefault()?.CategoryName;
        }

        if (criteria.UniversityId.HasValue)
        {
            model.UniversityName = model.BrowseUniversities.FirstOrDefault(u => u.Id == criteria.UniversityId)?.Name
                ?? model.Browse.Items.FirstOrDefault()?.UniversityName;
        }

        if (criteria.SubjectId.HasValue)
        {
            model.SubjectName = model.BrowseSubjects.FirstOrDefault(s => s.Id == criteria.SubjectId)?.Name;
        }

        if (criteria.AcademicLevelId.HasValue)
        {
            model.AcademicLevelName = model.BrowseAcademicLevels.FirstOrDefault(a => a.Id == criteria.AcademicLevelId)?.Name;
        }

        await MarkSavedAsync(model.Browse.Items, model.Featured, model.Recent);
        await MarkCompareAsync(model.Browse.Items, model.Featured, model.Recent);
        await MarkSellerVerifiedAsync(model.Browse.Items, model.Featured, model.Recent);

        return View(model);
    }

    [HttpGet("details/{slug}")]
    public async Task<IActionResult> Details(string slug)
    {
        var listing = await _listingQueryService.GetPublicDetailsBySlugAsync(slug);
        if (listing is null)
        {
            return NotFound();
        }

        var sessionKey = $"{ViewedListingSessionKeyPrefix}{listing.Id}";
        if (HttpContext.Session.GetString(sessionKey) is null)
        {
            await _listingService.IncrementViewCountAsync(listing.Id);
            HttpContext.Session.SetString(sessionKey, "1");
            listing.ViewCount++; // reflect immediately without a second fetch
        }

        var model = new ListingDetailsViewModel
        {
            Listing = listing,
            SellerProfile = await _listingQueryService.GetSellerProfileAsync(listing.SellerId),
            RelatedListings = await _listingQueryService.GetRelatedListingsAsync(listing.Id, RelatedCount),
        };

        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.GetUserId()!;
            var ids = model.RelatedListings.Select(r => r.Id).Append(listing.Id).Distinct();
            var savedIds = await _savedListingService.GetSavedListingIdsAsync(userId, ids);

            listing.IsSaved = savedIds.Contains(listing.Id);
            foreach (var related in model.RelatedListings)
            {
                related.IsSaved = savedIds.Contains(related.Id);
            }
        }

        var compareIds = await GetCurrentCompareIdsAsync();
        listing.IsInCompare = compareIds.Contains(listing.Id);
        foreach (var related in model.RelatedListings)
        {
            related.IsInCompare = compareIds.Contains(related.Id);
        }

        await MarkSellerVerifiedAsync(model.RelatedListings);
        listing.IsSellerVerified = await _verificationQueryService.IsUserVerifiedAsync(listing.SellerId);
        if (model.SellerProfile is not null)
        {
            model.SellerProfile.IsVerified = listing.IsSellerVerified;
            var reputation = await _reviewQueryService.GetReputationAsync(listing.SellerId);
            model.SellerProfile.AverageRating = reputation.SellerAverageRating;
            model.SellerProfile.ReviewCount = reputation.SellerReviewCount;
        }

        return View(model);
    }

    [HttpGet("seller/{sellerId}")]
    public async Task<IActionResult> Seller(string sellerId, int pageNumber = 1)
    {
        var profile = await _listingQueryService.GetSellerProfileAsync(sellerId);
        if (profile is null)
        {
            return NotFound();
        }

        // Settings > Privacy: a seller who opted out of a public profile is only visible
        // to themselves and Admins — everyone else gets the same 404 as a nonexistent
        // seller, never a distinguishable "this profile is private" message.
        var isOwnerOrAdmin = User.GetUserId() == sellerId || User.IsInRole(Roles.Admin);
        if (!profile.IsPublicProfile && !isOwnerOrAdmin)
        {
            return NotFound();
        }

        var model = new SellerProfileViewModel
        {
            Seller = profile,
            Listings = await _listingQueryService.GetPublicSellerListingsAsync(sellerId, pageNumber, PageSize),
        };

        await MarkSavedAsync(model.Listings.Items);
        await MarkCompareAsync(model.Listings.Items);
        await MarkSellerVerifiedAsync(model.Listings.Items);
        model.Seller.IsVerified = await _verificationQueryService.IsUserVerifiedAsync(sellerId);
        var reputation = await _reviewQueryService.GetReputationAsync(sellerId);
        model.Seller.AverageRating = reputation.SellerAverageRating;
        model.Seller.ReviewCount = reputation.SellerReviewCount;

        var completedOrders = await _orderQueryService.GetSellerOrdersAsync(sellerId, OrderStatus.Completed, 1, 1);
        model.Seller.CompletedOrderCount = completedOrders.TotalCount;

        // Public-safe fields only, straight off the approved verification snapshot —
        // never the Government ID/private-document paths, which this DTO never carries.
        if (model.Seller.IsVerified)
        {
            var verification = await _verificationQueryService.GetCurrentStatusAsync(sellerId);
            if (verification is not null)
            {
                model.Seller.SellerTypeDisplay = verification.SellerTypeDisplay;
                model.Seller.InstitutionName = verification.InstitutionName;
                model.Seller.SellingCategoryDisplays = verification.SellingCategoryDisplays;
                model.Seller.VerifiedAtUtc = verification.ReviewedAtUtc;
            }
        }

        return View(model);
    }
}
