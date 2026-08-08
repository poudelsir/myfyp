using SajhaSikshya.Data.Enums;

namespace SajhaSikshya.DTOs.Verification;

/// <summary>
/// Presentation-safe projection of <see cref="Data.Entities.Verification.StudentVerification"/>
/// for student-facing views (current status, history) and the admin queue listing.
/// Deliberately omits <c>AdminNotes</c> — those are internal-only and never shown to
/// the student; the admin detail page uses a separate, richer DTO instead of this one.
/// </summary>
public class VerificationDto
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string StudentName { get; set; } = string.Empty;

    /// <summary>Null for every submission made after the Seller Verification refinement — University was dropped from the application form. Only populated on legacy pre-refinement rows.</summary>
    public int? UniversityId { get; set; }

    public string? UniversityName { get; set; }

    /// <summary>Null for every submission made after the Seller Verification refinement — dropped from the application form. Only populated on legacy pre-refinement rows.</summary>
    public string? StudentNumber { get; set; }

    public SellerType? SellerType { get; set; }

    public string? SellerTypeDisplay { get; set; }

    public GovernmentIdDocumentType? GovernmentIdDocumentType { get; set; }

    public string? GovernmentIdDocumentTypeDisplay { get; set; }

    public AcademicIdDocumentType? AcademicIdDocumentType { get; set; }

    public string? AcademicIdDocumentTypeDisplay { get; set; }

    public IReadOnlyList<SellingCategory> SellingCategories { get; set; } = Array.Empty<SellingCategory>();

    public IReadOnlyList<string> SellingCategoryDisplays { get; set; } = Array.Empty<string>();

    /// <summary>Whether an optional Academic Identity document was provided — the document itself is never exposed on this DTO, only through <see cref="Controllers.VerificationImagesController"/>.</summary>
    public bool HasAcademicId { get; set; }

    public VerificationStatus Status { get; set; }

    public string StatusDisplay { get; set; } = string.Empty;

    public DateTime SubmittedAtUtc { get; set; }

    public DateTime? ReviewedAtUtc { get; set; }

    public string? ReviewedByName { get; set; }

    public string? RejectionReason { get; set; }

    public VerificationAction? LastAction { get; set; }

    public string? LastActionDisplay { get; set; }
}
