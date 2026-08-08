using System.ComponentModel.DataAnnotations;
using SajhaSikshya.Constants;
using SajhaSikshya.Data.Entities.Catalog;
using SajhaSikshya.Data.Enums;

namespace SajhaSikshya.Data.Entities.Verification;

/// <summary>
/// One submission of a student's verification documents. A user's verification
/// history is every non-deleted row with their <see cref="UserId"/> — rows are never
/// overwritten or reused across a resubmission; see
/// <see cref="Services.Verification.VerificationService"/> for why. The most recent
/// row (by <see cref="SubmittedAtUtc"/>) is that user's "current" verification state.
/// </summary>
public class StudentVerification : BaseEntity
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// Nullable since the Seller Verification refinement dropped University from the
    /// application form entirely (Seller Type now covers "what kind of seller" more
    /// generally, not just university students) — kept only so the 7+ pre-refinement
    /// rows in history still display their original university.
    /// </summary>
    public int? UniversityId { get; set; }

    public University? University { get; set; }

    /// <summary>Nullable for the same reason as <see cref="UniversityId"/> — dropped from the current application form, retained for legacy history rows.</summary>
    [StringLength(VerificationConstants.MaximumStudentNumberLength, MinimumLength = VerificationConstants.MinimumStudentNumberLength)]
    public string? StudentNumber { get; set; }

    /// <summary>
    /// The mandatory government-issued identity document (Citizenship/National ID/
    /// Passport/Driving License) — renamed from <c>StudentIdImagePath</c> since it's no
    /// longer specifically a student ID. Stored under the private upload root (see
    /// <see cref="VerificationConstants.ImageStorageSubfolder"/>), never a public URL.
    /// Never rendered directly in a view's <c>&lt;img src&gt;</c> — always served through
    /// an authorized controller action, since the underlying static file has no access
    /// control of its own. See Phase 5's Security notes.
    /// </summary>
    [Required]
    [StringLength(300)]
    public string GovernmentIdImagePath { get; set; } = string.Empty;

    /// <summary>Which type of document <see cref="GovernmentIdImagePath"/> is. Nullable only for legacy rows submitted before this field existed.</summary>
    public GovernmentIdDocumentType? GovernmentIdDocumentType { get; set; }

    /// <summary>Optional academic identity document (Student/College/University/Teacher/Institution ID) — increases buyer confidence but is never required to submit or approve an application.</summary>
    [StringLength(300)]
    public string? AcademicIdImagePath { get; set; }

    /// <summary>Which type of document <see cref="AcademicIdImagePath"/> is, when provided.</summary>
    public AcademicIdDocumentType? AcademicIdDocumentType { get; set; }

    /// <summary>
    /// Mandatory recent photo of the applicant, used to visually match against the
    /// government ID during review. Nullable only for legacy rows submitted before this
    /// field existed — every new submission always sets it.
    /// </summary>
    [StringLength(300)]
    public string? ProfilePhotoImagePath { get; set; }

    /// <summary>Self-declared applicant category (student/teacher/institution/etc.) — context for admin review and marketplace insights, not an access-control tier. Nullable only for legacy rows.</summary>
    public SellerType? SellerType { get; set; }

    /// <summary>
    /// Comma-separated int values of <see cref="Data.Enums.SellingCategory"/> the
    /// applicant intends to sell — multi-select, for moderation/insights only. A plain
    /// delimited string rather than a join table since this data is never queried
    /// relationally, only displayed.
    /// </summary>
    [StringLength(VerificationConstants.MaximumSellingCategoriesCsvLength)]
    public string SellingCategoriesCsv { get; set; } = string.Empty;

    /// <summary>Whether the applicant accepted the anti-fraud Seller Declaration. Required (validated at the ViewModel level) for every new submission; defaults false for legacy rows that predate the declaration.</summary>
    public bool DeclarationAccepted { get; set; }

    public VerificationStatus Status { get; set; } = VerificationStatus.Pending;

    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? ReviewedAtUtc { get; set; }

    public string? ReviewedByUserId { get; set; }

    public ApplicationUser? ReviewedByUser { get; set; }

    [StringLength(VerificationConstants.MaximumRejectionReasonLength)]
    public string? RejectionReason { get; set; }

    /// <summary>Internal-only notes for other admins — never shown to the student.</summary>
    [StringLength(VerificationConstants.MaximumAdminNotesLength)]
    public string? AdminNotes { get; set; }

    /// <summary>Which review action produced the current <see cref="Status"/> — see <see cref="VerificationAction"/>. Null until reviewed.</summary>
    public VerificationAction? LastAction { get; set; }
}
