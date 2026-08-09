using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SajhaSikshya.Constants;
using SajhaSikshya.Data.Constants;
using SajhaSikshya.DTOs.Catalog;
using SajhaSikshya.Helpers;
using SajhaSikshya.Services.Interfaces.Catalog;
using SajhaSikshya.ViewModels.Admin.Catalog;
using SajhaSikshya.ViewModels.Admin.Shared;

namespace SajhaSikshya.Areas.Admin.Controllers;

/// <summary>Admin CRUD for <see cref="Data.Entities.Catalog.Category"/>. All business rules (slug uniqueness, cycle prevention) live in <see cref="ICategoryService"/>.</summary>
[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class CategoriesController : Controller
{
    private const int PageSize = PaginationConstants.DefaultPageSize;

    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm, int pageNumber = 1)
    {
        var page = await _categoryService.GetPagedAsync(searchTerm, pageNumber, PageSize);
        return View(new AdminListViewModel<CategoryDto> { Page = page, SearchTerm = searchTerm });
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new CategoryFormViewModel();
        await PopulateParentOptionsAsync(model, excludeCategoryId: null);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateParentOptionsAsync(model, excludeCategoryId: null);
            return View(model);
        }

        var result = await _categoryService.CreateAsync(model);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            await PopulateParentOptionsAsync(model, excludeCategoryId: null);
            return View(model);
        }

        TempData[AlertHelper.SuccessKey] = $"Category '{model.Name}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var category = await _categoryService.GetByIdAsync(id);
        if (category is null)
        {
            return NotFound();
        }

        var model = new CategoryFormViewModel
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            ParentCategoryId = category.ParentCategoryId,
            DisplayOrder = category.DisplayOrder,
            IsActive = category.IsActive,
            IconName = category.IconName,
            Description = category.Description,
        };

        await PopulateParentOptionsAsync(model, excludeCategoryId: id);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CategoryFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await PopulateParentOptionsAsync(model, excludeCategoryId: id);
            return View(model);
        }

        var result = await _categoryService.UpdateAsync(model);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            await PopulateParentOptionsAsync(model, excludeCategoryId: id);
            return View(model);
        }

        TempData[AlertHelper.SuccessKey] = $"Category '{model.Name}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _categoryService.DeleteAsync(id);
        TempData[result.Succeeded ? AlertHelper.SuccessKey : AlertHelper.ErrorKey] =
            result.Succeeded ? "Category deleted successfully." : result.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateParentOptionsAsync(CategoryFormViewModel model, int? excludeCategoryId)
    {
        var eligibleParents = await _categoryService.GetEligibleParentCategoriesAsync(excludeCategoryId);

        model.ParentCategoryOptions = eligibleParents
            .Select(c => new SelectListItem(c.Name, c.Id.ToString()) { Selected = c.Id == model.ParentCategoryId })
            .ToList();
    }
}
