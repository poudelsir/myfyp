namespace SajhaSikshya.DTOs.Verification;

/// <summary>
/// Admin-only detail view of a verification request — everything in
/// <see cref="VerificationDto"/> plus the fields a student should never see
/// (<see cref="AdminNotes"/>) and a couple of extra identifying fields useful during
/// review. A superset of <see cref="VerificationDto"/>'s fields with nothing removed,
/// so inheritance avoids duplicating the base set rather than being a red flag here.
/// </summary>
public class VerificationDetailDto : VerificationDto
{
    public string StudentEmail { get; set; } = string.Empty;

    /// <summary>Internal-only notes for other admins — never shown to the student. Only ever populated on this admin-facing DTO, never on the base <see cref="VerificationDto"/>.</summary>
    public string? AdminNotes { get; set; }
}
