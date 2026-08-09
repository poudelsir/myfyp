using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using SajhaSikshya.Constants;
using SajhaSikshya.Data.Enums;
using SajhaSikshya.DTOs.Catalog;
using SajhaSikshya.DTOs.Marketplace;

namespace SajhaSikshya.ViewModels.Marketplace;

/// <summary>
/// Bound by both the Create and Edit forms for <c>Areas/Student/Controllers/ListingsController</c>.
/// Length limits reference <see cref="ListingConstants"/> directly (both are compile-time
/// constants, so this stays in sync automatically if the limits ever change) — the price
/// ceiling can't be expressed the same way in a DataAnnotation (it needs a runtime decimal
/// comparison), so that check lives in <see cref="Services.Marketplace.ListingService"/> instead.
/// </summary>
public class ListingFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Title is required.")]
    [StringLength(ListingConstants.MaximumTitleLength, MinimumLength = ListingConstants.MinimumTitleLength,
        ErrorMessage = "Title must be between {2} and {1} characters.")]
    [Display(Name = "Title")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required.")]
    [StringLength(ListingConstants.MaximumDescriptionLength, MinimumLength = 20,
        ErrorMessage = "Description must be between {2} and {1} characters.")]
    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter a price.")]
    [Range(0, double.MaxValue, ErrorMessage = "Price cannot be negative.")]
    [DataType(DataType.Currency)]
    [Display(Name = "Price (NPR)")]
    public decimal PriceAmount { get; set; }

    [Display(Name = "This is a free donation")]
    public bool IsDonation { get; set; }

    /// <summary>
    /// Zero is allowed at this annotation level (that's how an Edit/Restock call marks a
    /// listing manually out of stock); Create additionally requires at least 1, enforced
    /// server-side in <see cref="Services.Marketplace.ListingService.CreateAsync"/> since
    /// the same field/form serves both Create and Edit with different minimums.
    /// </summary>
    [Required(ErrorMessage = "Please enter a stock quantity.")]
    [Range(0, ListingConstants.MaximumStockQuantity, ErrorMessage = "Stock must be between 0 and {2}.")]
    [Display(Name = "Stock Quantity")]
    public int StockQuantity { get; set; }

    [Required(ErrorMessage = "Please select the item's condition.")]
    [Display(Name = "Condition")]
    public BookCondition Condition { get; set; }

    /// <summary>
    /// The submitted value is always the chosen SUBCATEGORY (leaf) — <see cref="DepartmentId"/>
    /// is a client-side-only narrowing aid for the Subcategory picker, never itself stored on
    /// the listing. See <see cref="Areas.Student.Controllers.ListingsController.PopulateDropdownsAsync"/>.
    /// </summary>
    [Required(ErrorMessage = "Please select a subcategory.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a subcategory.")]
    [Display(Name = "Subcategory")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Please select a subject.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a subject.")]
    [Display(Name = "Subject")]
    public int SubjectId { get; set; }

    /// <summary>Not bound/submitted — derived server-side from <see cref="CategoryId"/>'s parent on Edit, or set by the seller's client-side selection purely to narrow the Subcategory dropdown.</summary>
    [Display(Name = "Department")]
    public int? DepartmentId { get; set; }

    public IEnumerable<SelectListItem> DepartmentOptions { get; set; } = Array.Empty<SelectListItem>();

    /// <summary>Every active subcategory (children only, all departments) — filtered client-side to the selected <see cref="DepartmentId"/> rather than round-tripped via AJAX, since the whole taxonomy is small (~65 rows).</summary>
    public IReadOnlyList<CategoryDto> SubcategoryOptions { get; set; } = Array.Empty<CategoryDto>();

    public IEnumerable<SelectListItem> SubjectOptions { get; set; } = Array.Empty<SelectListItem>();

    /// <summary>
    /// Seller-typed university name — optional free text, matched case-insensitively
    /// against existing universities or auto-created if new (see
    /// <see cref="Services.Interfaces.Catalog.IUniversityService.FindOrCreateByNameAsync"/>).
    /// Independent of <see cref="Subject"/>'s own optional University — a listing belongs
    /// to whichever university the seller is actually at, which a shared/generic subject
    /// like "Calculus I" shouldn't dictate. Left blank, the listing keeps whatever
    /// university (if any) the chosen subject already implies, unchanged from before this
    /// field existed.
    /// </summary>
    [StringLength(200, ErrorMessage = "University name is too long.")]
    [Display(Name = "University")]
    public string? UniversityName { get; set; }

    /// <summary>Every active university's name, for the client-side typeahead — the whole list is small enough to embed inline, same reasoning as <see cref="SubcategoryOptions"/>.</summary>
    public IReadOnlyList<string> UniversityNameOptions { get; set; } = Array.Empty<string>();

    /// <summary>Only populated on Edit (a new listing being Created has no id to attach photos to yet); used by Edit.cshtml's image management panel.</summary>
    public IReadOnlyList<ListingImageDto> Images { get; set; } = Array.Empty<ListingImageDto>();
}
