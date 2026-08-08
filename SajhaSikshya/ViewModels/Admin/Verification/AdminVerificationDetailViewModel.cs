using SajhaSikshya.DTOs;
using SajhaSikshya.DTOs.Verification;

namespace SajhaSikshya.ViewModels.Admin.Verification;

/// <summary>Backs the Admin verification review page — the request under review plus the same student's other past attempts, useful context for a resubmission (why did it get rejected last time?).</summary>
public class AdminVerificationDetailViewModel
{
    public VerificationDetailDto Verification { get; set; } = null!;

    public PagedResult<VerificationDto> History { get; set; } = new();
}
