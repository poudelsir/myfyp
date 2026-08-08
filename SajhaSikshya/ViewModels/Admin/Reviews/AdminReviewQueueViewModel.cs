using SajhaSikshya.DTOs;
using SajhaSikshya.DTOs.Reviews;

namespace SajhaSikshya.ViewModels.Admin.Reviews;

/// <summary>Backs the Admin Review moderation queue.</summary>
public class AdminReviewQueueViewModel
{
    public bool ReportedOnly { get; set; } = true;

    public PagedResult<ReviewDto> Page { get; set; } = new();
}
