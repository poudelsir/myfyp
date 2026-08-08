using SajhaSikshya.DTOs.Marketplace;
using SajhaSikshya.DTOs.Verification;

namespace SajhaSikshya.ViewModels.Admin.Shared;

/// <summary>Backs the Admin landing dashboard — combines the Marketplace moderation stats (Milestone 3.3b) with the Student Verification stats (Phase 5.2), added alongside rather than replacing the existing listing section.</summary>
public class AdminDashboardViewModel
{
    public ListingModerationStatsDto ListingStats { get; set; } = new();

    public VerificationStatisticsDto VerificationStats { get; set; } = new();

    /// <summary>Reviews with ReportCount &gt; 0 awaiting moderator attention — reuses <c>IReviewQueryService.GetAdminQueueAsync</c>'s own TotalCount rather than a dedicated stats query.</summary>
    public int ReportedReviewCount { get; set; }
}
