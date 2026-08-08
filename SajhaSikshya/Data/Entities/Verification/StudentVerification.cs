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

    public int UniversityId { get; set; }

    public University University { get; set; } = null!;

    [Required]
    [StringLength(VerificationConstants.MaximumStudentNumberLength, MinimumLength = VerificationConstants.MinimumStudentNumberLength)]
    public string StudentNumber { get; set; } = string.Empty;

    /// <summary>
    /// Web-relative path under <c>wwwroot/uploads/</c> (see <see cref="VerificationConstants.ImageStorageSubfolder"/>).
    /// Never rendered directly in a view's <c>&lt;img src&gt;</c> — always served through
    /// an authorized controller action, since the underlying static file has no access
    /// control of its own. See Phase 5's Security notes.
    /// </summary>
    [Required]
    [StringLength(300)]
    public string StudentIdImagePath { get; set; } = string.Empty;

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
