using SajhaSikshya.Data.Entities.Verification;
using SajhaSikshya.Data.Enums;
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

    /// <summary>Admin-only variant of <see cref="ToDto"/> — same base fields (via the shared <see cref="PopulateBase"/> helper, so they can never drift apart) plus <c>AdminNotes</c>/<c>StudentEmail</c>/<c>StudentPhoneNumber</c>. Callers must also Include User for these.</summary>
    public static VerificationDetailDto ToDetailDto(this StudentVerification verification)
    {
        var dto = new VerificationDetailDto
        {
            StudentEmail = verification.User?.Email ?? string.Empty,
            StudentPhoneNumber = verification.User?.PhoneNumber,
            AdminNotes = verification.AdminNotes,
        };
        PopulateBase(dto, verification);
        return dto;
    }

    private static void PopulateBase(VerificationDto dto, StudentVerification verification)
    {
        var sellingCategories = ParseSellingCategories(verification.SellingCategoriesCsv);

        dto.Id = verification.Id;
        dto.UserId = verification.UserId;
        dto.StudentName = verification.User?.FullName ?? string.Empty;
        dto.UniversityId = verification.UniversityId;
        dto.UniversityName = verification.University?.Name;
        dto.StudentNumber = verification.StudentNumber;
        dto.SellerType = verification.SellerType;
        dto.SellerTypeDisplay = verification.SellerType?.GetDisplayName();
        dto.GovernmentIdDocumentType = verification.GovernmentIdDocumentType;
        dto.GovernmentIdDocumentTypeDisplay = verification.GovernmentIdDocumentType?.GetDisplayName();
        dto.AcademicIdDocumentType = verification.AcademicIdDocumentType;
        dto.AcademicIdDocumentTypeDisplay = verification.AcademicIdDocumentType?.GetDisplayName();
        dto.SellingCategories = sellingCategories;
        dto.SellingCategoryDisplays = sellingCategories.Select(c => c.GetDisplayName()).ToList();
        dto.HasAcademicId = !string.IsNullOrEmpty(verification.AcademicIdImagePath);
        dto.Status = verification.Status;
        dto.StatusDisplay = verification.Status.GetDisplayName();
        dto.SubmittedAtUtc = verification.SubmittedAtUtc;
        dto.ReviewedAtUtc = verification.ReviewedAtUtc;
        dto.ReviewedByName = verification.ReviewedByUser?.FullName;
        dto.RejectionReason = verification.RejectionReason;
        dto.LastAction = verification.LastAction;
        dto.LastActionDisplay = verification.LastAction?.GetDisplayName();
    }

    /// <summary>Parses <see cref="StudentVerification.SellingCategoriesCsv"/> back into enum values, silently skipping any token that isn't a valid <see cref="SellingCategory"/> int rather than throwing — display code should never crash on stored data.</summary>
    private static List<SellingCategory> ParseSellingCategories(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return new List<SellingCategory>();
        }

        var result = new List<SellingCategory>();
        foreach (var token in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (int.TryParse(token, out var value) && Enum.IsDefined(typeof(SellingCategory), value))
            {
                result.Add((SellingCategory)value);
            }
        }

        return result;
    }
}
