using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SajhaSikshya.Authorization;
using SajhaSikshya.Constants;
using SajhaSikshya.Data.Constants;
using SajhaSikshya.Data.Enums;
using SajhaSikshya.DTOs.Marketplace;
using SajhaSikshya.Extensions;
using SajhaSikshya.Helpers;
using SajhaSikshya.Services.AI;
using SajhaSikshya.Services.AI.Prompts;
using SajhaSikshya.Services.Interfaces.AI;
using SajhaSikshya.Services.Interfaces.Catalog;
using SajhaSikshya.Services.Interfaces.Marketplace;
using SajhaSikshya.ViewModels.Marketplace;
using SajhaSikshya.ViewModels.Student.Marketplace;

namespace SajhaSikshya.Areas.Student.Controllers;

/// <summary>
/// Seller-facing listing management ("seller" here just means a Student creating a
/// marketplace listing — there is no separate Seller role). Mutations (ownership,
/// price limits, status transitions, image management) live in <see cref="IListingService"/>;
/// reads go through <see cref="IListingQueryService"/>.
/// </summary>
[Area("Student")]
[Authorize(Roles = Roles.Student)]
[Authorize(Policy = AuthorizationPolicies.VerifiedStudent)]
public class ListingsController : Controller
{
    private const int PageSize = PaginationConstants.DefaultPageSize;

    private readonly IListingService _listingService;
    private readonly IListingQueryService _listingQueryService;
    private readonly ICategoryService _categoryService;
    private readonly ISubjectService _subjectService;
    private readonly IListingAIService _listingAIService;

    public ListingsController(
        IListingService listingService,
        IListingQueryService listingQueryService,
        ICategoryService categoryService,
        ISubjectService subjectService,
        IListingAIService listingAIService)
    {
        _listingService = listingService;
        _listingQueryService = listingQueryService;
        _categoryService = categoryService;
        _subjectService = subjectService;
        _listingAIService = listingAIService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm, ListingStatus? status, int? categoryId, int pageNumber = 1)
    {
        var sellerId = User.GetUserId()!;
        var page = await _listingQueryService.GetMyListingsAsync(sellerId, searchTerm, status, categoryId, pageNumber, PageSize);

        var departments = await _categoryService.GetEligibleParentCategoriesAsync(excludeCategoryId: null);
        var categoryOptions = departments
            .Where(c => c.ParentCategoryId is null)
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem(c.Name, c.Id.ToString()) { Selected = c.Id == categoryId });

        return View(new StudentListingListViewModel
        {
            Page = page,
            SearchTerm = searchTerm,
            Status = status,
            CategoryId = categoryId,
            CategoryOptions = categoryOptions,
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new ListingFormViewModel();
        await PopulateDropdownsAsync(model);
        return View(model);
    }

    // See UploadImages below for why the Kestrel request-size limit is disabled here too —
    // Create can now carry the same up-to-MaximumImages x MaximumImageSizeMB payload inline.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> Create(ListingFormViewModel model, List<IFormFile>? photos)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(model);
            return View(model);
        }

        var sellerId = User.GetUserId()!;
        var result = await _listingService.CreateAsync(sellerId, model);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            await PopulateDropdownsAsync(model);
            return View(model);
        }

        if (photos is { Count: > 0 })
        {
            var uploadResult = await _listingService.UploadImagesAsync(sellerId, result.Data, photos);
            if (!uploadResult.Succeeded)
            {
                TempData[AlertHelper.ErrorKey] =
                    $"Listing created, but photos could not be uploaded: {uploadResult.Errors.FirstOrDefault()}. You can add them here.";
                return RedirectToAction(nameof(Edit), new { id = result.Data });
            }
        }

        TempData[AlertHelper.SuccessKey] = $"Listing '{model.Title}' created and submitted for review.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var sellerId = User.GetUserId()!;
        var listing = await _listingQueryService.GetForSellerAsync(sellerId, id);
        if (listing is null)
        {
            return NotFound();
        }

        var model = new ListingFormViewModel
        {
            Id = listing.Id,
            Title = listing.Title,
            Description = listing.Description,
            PriceAmount = listing.PriceAmount,
            IsDonation = listing.IsDonation,
            Condition = listing.Condition,
            CategoryId = listing.CategoryId,
            SubjectId = listing.SubjectId,
            StockQuantity = listing.StockQuantity,
            Images = listing.Images,
        };

        await PopulateDropdownsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ListingFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var sellerId = User.GetUserId()!;

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(model);
            model.Images = await GetImagesAsync(sellerId, id);
            return View(model);
        }

        var result = await _listingService.UpdateAsync(sellerId, model);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            await PopulateDropdownsAsync(model);
            model.Images = await GetImagesAsync(sellerId, id);
            return View(model);
        }

        TempData[AlertHelper.SuccessKey] = "Listing updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Preview(int id)
    {
        var sellerId = User.GetUserId()!;
        var listing = await _listingQueryService.GetForSellerAsync(sellerId, id);
        if (listing is null)
        {
            return NotFound();
        }

        return View(listing);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var sellerId = User.GetUserId()!;
        var result = await _listingService.DeleteAsync(sellerId, id);
        TempData[result.Succeeded ? AlertHelper.SuccessKey : AlertHelper.ErrorKey] =
            result.Succeeded ? "Listing deleted." : result.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(int id)
    {
        var sellerId = User.GetUserId()!;
        var result = await _listingService.ArchiveAsync(sellerId, id);
        TempData[result.Succeeded ? AlertHelper.SuccessKey : AlertHelper.ErrorKey] =
            result.Succeeded ? "Listing archived." : result.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id)
    {
        var sellerId = User.GetUserId()!;
        var result = await _listingService.RestoreAsync(sellerId, id);
        TempData[result.Succeeded ? AlertHelper.SuccessKey : AlertHelper.ErrorKey] =
            result.Succeeded ? "Listing restored." : result.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restock(int id, int stockQuantity)
    {
        var sellerId = User.GetUserId()!;
        var result = await _listingService.UpdateStockAsync(sellerId, id, stockQuantity);
        TempData[result.Succeeded ? AlertHelper.SuccessKey : AlertHelper.ErrorKey] =
            result.Succeeded ? "Stock updated." : result.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Index));
    }

    // Kestrel's default per-request body limit (~28.6 MB) can be smaller than
    // MaximumImages x MaximumImageSizeMB. Disabling it here is safe because
    // ImageStorageService independently enforces the real per-file size limit, and
    // ListingService independently enforces the per-listing image count — those are
    // the authoritative checks, not the transport-level cap.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> UploadImages(int id, List<IFormFile> images)
    {
        var sellerId = User.GetUserId()!;
        var result = await _listingService.UploadImagesAsync(sellerId, id, images);
        TempData[result.Succeeded ? AlertHelper.SuccessKey : AlertHelper.ErrorKey] =
            result.Succeeded ? "Photos uploaded." : result.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> ReplaceImage(int imageId, int listingId, IFormFile newImage)
    {
        var sellerId = User.GetUserId()!;
        var result = await _listingService.ReplaceImageAsync(sellerId, imageId, newImage);
        TempData[result.Succeeded ? AlertHelper.SuccessKey : AlertHelper.ErrorKey] =
            result.Succeeded ? "Photo replaced." : result.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Edit), new { id = listingId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(int imageId, int listingId)
    {
        var sellerId = User.GetUserId()!;
        var result = await _listingService.DeleteImageAsync(sellerId, imageId);
        TempData[result.Succeeded ? AlertHelper.SuccessKey : AlertHelper.ErrorKey] =
            result.Succeeded ? "Photo deleted." : result.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Edit), new { id = listingId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetThumbnail(int imageId, int listingId)
    {
        var sellerId = User.GetUserId()!;
        var result = await _listingService.SetThumbnailAsync(sellerId, imageId);
        TempData[result.Succeeded ? AlertHelper.SuccessKey : AlertHelper.ErrorKey] =
            result.Succeeded ? "Thumbnail updated." : result.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Edit), new { id = listingId });
    }

    /// <summary>Called via fetch() after a drag-and-drop reorder — returns a plain status instead of redirecting since the gallery is already reordered client-side.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReorderImages(int listingId, List<int> orderedImageIds)
    {
        var sellerId = User.GetUserId()!;
        var result = await _listingService.ReorderImagesAsync(sellerId, listingId, orderedImageIds);
        return result.Succeeded ? Ok() : BadRequest(result.Errors.FirstOrDefault());
    }

    /// <summary>
    /// Called via fetch() from the "Generate with AI" button on the Create/Edit form.
    /// Category/Subject are resolved server-side from their ids (never trusting a
    /// display name posted from the client) before being handed to
    /// <see cref="IListingAIService"/> — the only client-supplied free text that
    /// reaches the prompt is the seller's own working title.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateDescription(string? title, BookCondition condition, int categoryId, int subjectId, decimal priceAmount, bool isDonation)
    {
        var category = await _categoryService.GetByIdAsync(categoryId);
        var subject = await _subjectService.GetByIdAsync(subjectId);
        if (category is null || subject is null)
        {
            return BadRequest("Please select a category and subject first.");
        }

        var input = new ListingDescriptionPromptInput(title, condition, category.Name, subject.Name, subject.AcademicLevelName, priceAmount, isDonation);
        var result = await _listingAIService.GenerateDescriptionAsync(User.GetUserId()!, input);
        if (!result.Succeeded || result.Data is null)
        {
            return BadRequest(result.Errors.FirstOrDefault() ?? "Could not generate a description right now.");
        }

        return Json(result.Data);
    }

    /// <summary>
    /// Called via fetch() from the "AI Price Recommendation" button. <paramref name="id"/>
    /// is 0 on Create (nothing to exclude) or the listing's own id on Edit (excluded from
    /// its own comparable-listings query — see <see cref="IListingAIService.RecommendPriceAsync"/>).
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecommendPrice(int id, string title, string description, BookCondition condition, int categoryId, int subjectId, bool isDonation)
    {
        var category = await _categoryService.GetByIdAsync(categoryId);
        var subject = await _subjectService.GetByIdAsync(subjectId);
        if (category is null || subject is null)
        {
            return BadRequest("Please select a category and subject first.");
        }

        var request = new ListingPriceRecommendationRequest(
            title,
            description,
            condition,
            categoryId,
            category.Name,
            subjectId,
            subject.Name,
            subject.AcademicLevelId,
            subject.AcademicLevelName,
            subject.UniversityName,
            isDonation,
            id == 0 ? null : id);

        var result = await _listingAIService.RecommendPriceAsync(User.GetUserId()!, request);
        if (!result.Succeeded || result.Data is null)
        {
            return BadRequest(result.Errors.FirstOrDefault() ?? "Could not generate a price recommendation right now.");
        }

        return Json(result.Data);
    }

    /// <summary>Re-fetches a listing's current images for redisplay when the Edit form is re-rendered after a validation error (the POST body carries no image data).</summary>
    private async Task<IReadOnlyList<ListingImageDto>> GetImagesAsync(string sellerId, int listingId)
    {
        var listing = await _listingQueryService.GetForSellerAsync(sellerId, listingId);
        return listing?.Images ?? Array.Empty<ListingImageDto>();
    }

    private async Task PopulateDropdownsAsync(ListingFormViewModel model)
    {
        // No excludeCategoryId (that parameter exists for the Admin cycle-prevention picker) —
        // passing null returns every active category, which is what a listing's own picker needs.
        var categories = await _categoryService.GetEligibleParentCategoriesAsync(excludeCategoryId: null);
        var subjects = await _subjectService.GetAllActiveAsync();

        var departments = categories.Where(c => c.ParentCategoryId is null).OrderBy(c => c.Name).ToList();
        var subcategories = categories.Where(c => c.ParentCategoryId is not null).OrderBy(c => c.Name).ToList();

        // On Edit, the listing already has a leaf CategoryId — derive which department it
        // belongs to so the Department select starts on the right value (the Subcategory
        // select itself is filtered/populated entirely client-side, see _Form.cshtml).
        if (model.CategoryId > 0)
        {
            model.DepartmentId = subcategories.FirstOrDefault(c => c.Id == model.CategoryId)?.ParentCategoryId;
        }

        model.DepartmentOptions = departments
            .Select(c => new SelectListItem(c.Name, c.Id.ToString()) { Selected = c.Id == model.DepartmentId })
            .ToList();

        model.SubcategoryOptions = subcategories;

        model.SubjectOptions = subjects
            .Select(s => new SelectListItem(s.Name, s.Id.ToString()) { Selected = s.Id == model.SubjectId })
            .ToList();
    }
}
