namespace SajhaSikshya.DTOs.Verification;

/// <summary>Aggregate counts and review-throughput metrics for the Admin Verification Dashboard.</summary>
public class VerificationStatisticsDto
{
    public int PendingCount { get; set; }

    public int ApprovedCount { get; set; }

    public int RejectedCount { get; set; }

    /// <summary>Approved / (Approved + Rejected) as a percentage, rounded to 1 decimal place. Zero if nothing has been reviewed yet (rather than dividing by zero).</summary>
    public double ApprovalRatePercent { get; set; }

    /// <summary>Average time (in hours) between a request being submitted and reviewed, across every reviewed request. Null if nothing has been reviewed yet.</summary>
    public double? AverageReviewTimeHours { get; set; }

    public IReadOnlyList<VerificationDto> RecentReviews { get; set; } = Array.Empty<VerificationDto>();
}
