using SajhaSikshya.Data.Enums;
using SajhaSikshya.DTOs;
using SajhaSikshya.DTOs.Verification;

namespace SajhaSikshya.ViewModels.Admin.Verification;

/// <summary>Backs the Admin Verification queue — mirrors <c>AdminListingListViewModel</c>'s shape exactly.</summary>
public class AdminVerificationListViewModel
{
    public PagedResult<VerificationDto> Page { get; set; } = new();

    public string? SearchTerm { get; set; }

    public VerificationStatus? Status { get; set; }
}
