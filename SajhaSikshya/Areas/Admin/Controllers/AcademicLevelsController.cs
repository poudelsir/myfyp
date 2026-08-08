using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SajhaSikshya.Constants;
using SajhaSikshya.Data.Constants;
using SajhaSikshya.DTOs.Catalog;
using SajhaSikshya.Helpers;
using SajhaSikshya.Services.Interfaces.Catalog;
using SajhaSikshya.ViewModels.Admin.Catalog;
using SajhaSikshya.ViewModels.Admin.Shared;

namespace SajhaSikshya.Areas.Admin.Controllers;

/// <summary>Admin CRUD for <see cref="Data.Entities.Catalog.AcademicLevel"/>. All business rules live in <see cref="IAcademicLevelService"/>.</summary>
[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class AcademicLevelsController : Controller
{
    private const int PageSize = PaginationConstants.DefaultPageSize;

    private readonly IAcademicLevelService _academicLevelService;

    public AcademicLevelsController(IAcademicLevelService academicLevelService)
    {
        _academicLevelService = academicLevelService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm, int pageNumber = 1)
    {
        var page = await _academicLevelService.GetPagedAsync(searchTerm, pageNumber, PageSize);
        return View(new AdminListViewModel<AcademicLevelDto> { Page = page, SearchTerm = searchTerm });
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new AcademicLevelFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AcademicLevelFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _academicLevelService.CreateAsync(model);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return View(model);
        }

        TempData[AlertHelper.SuccessKey] = $"Academic level '{model.Name}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var level = await _academicLevelService.GetByIdAsync(id);
        if (level is null)
        {
            return NotFound();
        }

        return View(new AcademicLevelFormViewModel
        {
            Id = level.Id,
            Name = level.Name,
            Code = level.Code,
            DisplayOrder = level.DisplayOrder,
            IsActive = level.IsActive,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AcademicLevelFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _academicLevelService.UpdateAsync(model);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return View(model);
        }

        TempData[AlertHelper.SuccessKey] = $"Academic level '{model.Name}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _academicLevelService.DeleteAsync(id);
        TempData[result.Succeeded ? AlertHelper.SuccessKey : AlertHelper.ErrorKey] =
            result.Succeeded ? "Academic level deleted successfully." : result.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Index));
    }
}
