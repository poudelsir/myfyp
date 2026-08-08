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

/// <summary>Admin CRUD for <see cref="Data.Entities.Catalog.University"/>. All business rules live in <see cref="IUniversityService"/>.</summary>
[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class UniversitiesController : Controller
{
    private const int PageSize = PaginationConstants.DefaultPageSize;

    private readonly IUniversityService _universityService;

    public UniversitiesController(IUniversityService universityService)
    {
        _universityService = universityService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm, int pageNumber = 1)
    {
        var page = await _universityService.GetPagedAsync(searchTerm, pageNumber, PageSize);
        return View(new AdminListViewModel<UniversityDto> { Page = page, SearchTerm = searchTerm });
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new UniversityFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UniversityFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _universityService.CreateAsync(model);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return View(model);
        }

        TempData[AlertHelper.SuccessKey] = $"University '{model.Name}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var university = await _universityService.GetByIdAsync(id);
        if (university is null)
        {
            return NotFound();
        }

        return View(new UniversityFormViewModel
        {
            Id = university.Id,
            Name = university.Name,
            Code = university.Code,
            City = university.City,
            IsActive = university.IsActive,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UniversityFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _universityService.UpdateAsync(model);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return View(model);
        }

        TempData[AlertHelper.SuccessKey] = $"University '{model.Name}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _universityService.DeleteAsync(id);
        TempData[result.Succeeded ? AlertHelper.SuccessKey : AlertHelper.ErrorKey] =
            result.Succeeded ? "University deleted successfully." : result.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Index));
    }
}
