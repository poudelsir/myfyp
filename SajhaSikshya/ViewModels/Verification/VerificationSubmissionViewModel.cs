using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using SajhaSikshya.Data.Enums;

namespace SajhaSikshya.ViewModels.Verification;

/// <summary>
/// Bound from the Submit/Resubmit verification form. Both actions use the same shape
/// — a resubmission is a brand new document set, not an edit of the old one (see
/// <see cref="Services.Verification.VerificationService"/>'s remarks on why history is
/// never overwritten), so there's no meaningful difference in what the form collects.
/// Deliberately does not ask for name/email/phone — those already exist on the
/// account and are shown read-only via the <c>Account*</c> properties below.
/// </summary>
public class VerificationSubmissionViewModel
{
    [Required(ErrorMessage = "Please upload a government-issued identity document.")]
    public IFormFile? GovernmentIdImage { get; set; }

    [Required(ErrorMessage = "Please select which document you uploaded.")]
    public GovernmentIdDocumentType GovernmentIdDocumentType { get; set; }

    /// <summary>Optional — increases buyer confidence but is never required.</summary>
    public IFormFile? AcademicIdImage { get; set; }

    public AcademicIdDocumentType? AcademicIdDocumentType { get; set; }

    [Required(ErrorMessage = "Please upload a recent photo of yourself.")]
    public IFormFile? ProfilePhoto { get; set; }

    [Required(ErrorMessage = "Please select what best describes you.")]
    public SellerType SellerType { get; set; }

    /// <summary>Optional free-text school/college/university/company name — shown on the Seller Profile alongside Seller Type when provided.</summary>
    [StringLength(150)]
    public string? InstitutionName { get; set; }

    [MinLength(1, ErrorMessage = "Please select at least one category you plan to sell.")]
    public List<SellingCategory> SellingCategories { get; set; } = new();

    /// <summary>The standard ASP.NET Core "checkbox must be checked" validation trick — a bool can only equal <c>true</c> when compared against the range [true, true].</summary>
    [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept the declaration to continue.")]
    public bool DeclarationAccepted { get; set; }

    /// <summary>Populated by the controller for read-only display — never bound from the submitted form.</summary>
    public string AccountFullName { get; set; } = string.Empty;

    /// <summary>Populated by the controller for read-only display — never bound from the submitted form.</summary>
    public string AccountEmail { get; set; } = string.Empty;

    /// <summary>Populated by the controller for read-only display — never bound from the submitted form.</summary>
    public string? AccountPhoneNumber { get; set; }
}
