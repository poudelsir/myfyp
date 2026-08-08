using SajhaSikshya.DTOs;
using SajhaSikshya.DTOs.Verification;

namespace SajhaSikshya.ViewModels.Verification;

/// <summary>Backs the Student "My Verification" dashboard: the current status card/timeline plus the paged history of every past attempt.</summary>
public class VerificationDashboardViewModel
{
    /// <summary>The user's most recent verification request, or null if they've never submitted one.</summary>
    public VerificationDto? Current { get; set; }

    public PagedResult<VerificationDto> History { get; set; } = new();
}
