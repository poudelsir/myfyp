using SajhaSikshya.DTOs.Marketplace;
using SajhaSikshya.DTOs.Reviews;
using SajhaSikshya.DTOs.Verification;

namespace SajhaSikshya.DTOs.Dashboard;

/// <summary>
/// Everything the Student landing dashboard needs, scoped to one user — populated
/// entirely by composing existing query services plus a handful of bounded repository
/// counts. See <see cref="Services.Interfaces.Dashboard.IDashboardQueryService.GetStudentDashboardAsync"/>.
/// </summary>
public class StudentDashboardStatsDto
{
    public MyListingStatusCountsDto ListingStats { get; set; } = new();

    public MyOrderStatusCountsDto BuyerOrderStats { get; set; } = new();

    public MyOrderStatusCountsDto SellerOrderStats { get; set; } = new();

    public int SavedListingsCount { get; set; }

    public int CompareCount { get; set; }

    public int UnreadMessagesCount { get; set; }

    public int UnreadNotificationsCount { get; set; }

    public bool IsVerified { get; set; }

    public VerificationDto? Verification { get; set; }

    public ReputationDto Reputation { get; set; } = new();

    public int AIUsageThisMonthCount { get; set; }

    /// <summary>Active listings matching the student's university, excluding their own — capped at 6. Falls back to the marketplace's "Recent" rail when the student has no verification on file yet.</summary>
    public IReadOnlyList<ListingSummaryDto> RecommendedListings { get; set; } = Array.Empty<ListingSummaryDto>();

    /// <summary>Merged, time-sorted events across Listings/Orders/Reviews/Verification/Notifications — capped at 10.</summary>
    public IReadOnlyList<RecentActivityItemDto> RecentActivity { get; set; } = Array.Empty<RecentActivityItemDto>();
}
