using SajhaSikshya.Data.Entities.Verification;
using SajhaSikshya.DTOs.Verification;
using SajhaSikshya.Extensions;

namespace SajhaSikshya.Mappings.Verification;

public static class VerificationMappings
{
    /// <summary>Callers must Include User/University/ReviewedByUser first — this only reads off already-loaded navigation properties, it doesn't query.</summary>
    public static VerificationDto ToDto(this StudentVerification verification)
    {
        var dto = new VerificationDto();
        PopulateBase(dto, verification);
        return dto;
    }

    /// <summary>Admin-only variant of <see cref="ToDto"/> — same base fields (via the shared <see cref="PopulateBase"/> helper, so they can never drift apart) plus <c>AdminNotes</c>/<c>StudentEmail</c>. Callers must also Include User for the email.</summary>
    public static VerificationDetailDto ToDetailDto(this StudentVerification verification)
    {
        var dto = new VerificationDetailDto
        {
            StudentEmail = verification.User?.Email ?? string.Empty,
            AdminNotes = verification.AdminNotes,
        };
        PopulateBase(dto, verification);
        return dto;
    }

    private static void PopulateBase(VerificationDto dto, StudentVerification verification)
    {
        dto.Id = verification.Id;
        dto.UserId = verification.UserId;
        dto.StudentName = verification.User?.FullName ?? string.Empty;
        dto.UniversityId = verification.UniversityId;
        dto.UniversityName = verification.University?.Name ?? string.Empty;
        dto.StudentNumber = verification.StudentNumber;
        dto.Status = verification.Status;
        dto.StatusDisplay = verification.Status.GetDisplayName();
        dto.SubmittedAtUtc = verification.SubmittedAtUtc;
        dto.ReviewedAtUtc = verification.ReviewedAtUtc;
        dto.ReviewedByName = verification.ReviewedByUser?.FullName;
        dto.RejectionReason = verification.RejectionReason;
        dto.LastAction = verification.LastAction;
        dto.LastActionDisplay = verification.LastAction?.GetDisplayName();
    }
}
