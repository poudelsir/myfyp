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

    public int UniversityId { get; set; }

    public string UniversityName { get; set; } = string.Empty;

    public string StudentNumber { get; set; } = string.Empty;

    public VerificationStatus Status { get; set; }

    public string StatusDisplay { get; set; } = string.Empty;

    public DateTime SubmittedAtUtc { get; set; }

    public DateTime? ReviewedAtUtc { get; set; }

    public string? ReviewedByName { get; set; }

    public string? RejectionReason { get; set; }

    public VerificationAction? LastAction { get; set; }

    public string? LastActionDisplay { get; set; }
}
