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

/// <summary>Admin CRUD for <see cref="Data.Entities.Catalog.Subject"/>. All business rules live in <see cref="ISubjectService"/>.</summary>
[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class SubjectsController : Controller
{
    private const int PageSize = PaginationConstants.DefaultPageSize;

    private readonly ISubjectService _subjectService;
    private readonly IAcademicLevelService _academicLevelService;
    private readonly IUniversityService _universityService;

    public SubjectsController(
        ISubjectService subjectService,
        IAcademicLevelService academicLevelService,
        IUniversityService universityService)
    {
        _subjectService = subjectService;
        _academicLevelService = academicLevelService;
        _universityService = universityService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm, int pageNumber = 1)
    {
        var page = await _subjectService.GetPagedAsync(searchTerm, pageNumber, PageSize);
        return View(new AdminListViewModel<SubjectDto> { Page = page, SearchTerm = searchTerm });
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new SubjectFormViewModel();
        await PopulateDropdownsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SubjectFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(model);
            return View(model);
        }

        var result = await _subjectService.CreateAsync(model);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            await PopulateDropdownsAsync(model);
            return View(model);
        }

        TempData[AlertHelper.SuccessKey] = $"Subject '{model.Name}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var subject = await _subjectService.GetByIdAsync(id);
        if (subject is null)
        {
            return NotFound();
        }

        var model = new SubjectFormViewModel
        {
            Id = subject.Id,
            Name = subject.Name,
            Code = subject.Code,
            AcademicLevelId = subject.AcademicLevelId,
            UniversityId = subject.UniversityId,
            IsActive = subject.IsActive,
        };

        await PopulateDropdownsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SubjectFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(model);
            return View(model);
        }

        var result = await _subjectService.UpdateAsync(model);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            await PopulateDropdownsAsync(model);
            return View(model);
        }

        TempData[AlertHelper.SuccessKey] = $"Subject '{model.Name}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _subjectService.DeleteAsync(id);
        TempData[result.Succeeded ? AlertHelper.SuccessKey : AlertHelper.ErrorKey] =
            result.Succeeded ? "Subject deleted successfully." : result.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdownsAsync(SubjectFormViewModel model)
    {
        var levels = await _academicLevelService.GetAllActiveAsync();
        var universities = await _universityService.GetAllActiveAsync();

        model.AcademicLevelOptions = levels
            .Select(l => new SelectListItem(l.Name, l.Id.ToString()) { Selected = l.Id == model.AcademicLevelId })
            .ToList();

        model.UniversityOptions = universities
            .Select(u => new SelectListItem(u.Name, u.Id.ToString()) { Selected = u.Id == model.UniversityId })
            .ToList();
    }
}
