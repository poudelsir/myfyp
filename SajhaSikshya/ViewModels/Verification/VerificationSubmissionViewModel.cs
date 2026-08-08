using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using SajhaSikshya.Constants;

namespace SajhaSikshya.ViewModels.Verification;

/// <summary>
/// Bound from the Submit/Resubmit verification form. Both actions use the same shape
/// — a resubmission is a brand new document set, not an edit of the old one (see
/// <see cref="Services.Verification.VerificationService"/>'s remarks on why history is
/// never overwritten), so there's no meaningful difference in what the form collects.
/// </summary>
public class VerificationSubmissionViewModel
{
    [Required(ErrorMessage = "Please select your university.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select your university.")]
    public int UniversityId { get; set; }

    [Required(ErrorMessage = "Please enter your student number.")]
    [StringLength(VerificationConstants.MaximumStudentNumberLength, MinimumLength = VerificationConstants.MinimumStudentNumberLength)]
    public string StudentNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please upload a photo of your student ID.")]
    public IFormFile? StudentIdImage { get; set; }

    /// <summary>Populated by the controller before redisplaying the form (initial GET or a failed POST) — not bound from the submitted form itself. Same <c>SelectListItem</c>-list convention as <c>ListingFormViewModel.CategoryOptions</c>.</summary>
    public IEnumerable<SelectListItem> UniversityOptions { get; set; } = Array.Empty<SelectListItem>();
}
