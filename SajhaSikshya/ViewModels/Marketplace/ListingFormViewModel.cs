using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using SajhaSikshya.Constants;
using SajhaSikshya.Data.Enums;
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

    [Required(ErrorMessage = "Please select the item's condition.")]
    [Display(Name = "Condition")]
    public BookCondition Condition { get; set; }

    [Required(ErrorMessage = "Please select a category.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a category.")]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Please select a subject.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a subject.")]
    [Display(Name = "Subject")]
    public int SubjectId { get; set; }

    public IEnumerable<SelectListItem> CategoryOptions { get; set; } = Array.Empty<SelectListItem>();

    public IEnumerable<SelectListItem> SubjectOptions { get; set; } = Array.Empty<SelectListItem>();

    /// <summary>Only populated on Edit (a new listing being Created has no id to attach photos to yet); used by Edit.cshtml's image management panel.</summary>
    public IReadOnlyList<ListingImageDto> Images { get; set; } = Array.Empty<ListingImageDto>();
}
