using SajhaSikshya.DTOs.AI;
using SajhaSikshya.DTOs.Marketplace;
using SajhaSikshya.DTOs.Orders;
using SajhaSikshya.DTOs.Verification;

namespace SajhaSikshya.DTOs.Dashboard;

/// <summary>
/// Everything the Admin landing dashboard needs, platform-wide. Composes
/// <see cref="Services.Interfaces.AI.IAdminInsightsService.GetStatsAsync"/> for AI usage,
/// listing growth/top categories/universities, verification approval rate, platform
/// rating, notification totals, and donation counts rather than recomputing them — see
/// <see cref="Services.Interfaces.Dashboard.IDashboardQueryService.GetAdminDashboardAsync"/>.
/// </summary>
public class AdminDashboardStatsDto
{
    public ListingModerationStatsDto ListingStats { get; set; } = new();

    public VerificationStatisticsDto VerificationStats { get; set; } = new();

    public int ReportedReviewCount { get; set; }

    public AdminInsightsStatsDto Insights { get; set; } = new();

    /// <summary>The 4 <see cref="Data.Enums.ListingStatus"/> values <see cref="ListingStats"/>/<see cref="Insights"/> don't already cover.</summary>
    public int DraftListings { get; set; }

    public int ReservedListings { get; set; }

    /// <summary>Completed, non-donation orders platform-wide — NOT a count of <see cref="Data.Enums.ListingStatus.Sold"/> rows. Since Phase 11.5 (inventory/stock), a completed sale moves a listing to Active or OutOfStock, never Sold, so this is a sales-activity count, not a listing-status bucket.</summary>
    public int SoldListings { get; set; }

    public int OutOfStockListings { get; set; }

    public int TotalUsers { get; set; }

    public int TotalStudents { get; set; }

    public int TotalAdmins { get; set; }

    public int ActiveUsers { get; set; }

    public int UsersThisMonth { get; set; }

    public int UsersLastMonth { get; set; }

    /// <summary>Registrations for the last 6 months, oldest first — <c>Name</c> is a "MMM yyyy" label.</summary>
    public IReadOnlyList<NameCountDto> RegistrationGrowth { get; set; } = Array.Empty<NameCountDto>();

    public OrderStatisticsDto OrderStats { get; set; } = new();

    public decimal TotalRevenue { get; set; }

    public decimal RevenueThisMonth { get; set; }

    public decimal RevenueLastMonth { get; set; }

    public string RevenueCurrency { get; set; } = "NPR";

    /// <summary>Index 0 = count of 1-star reviews platform-wide ... index 4 = count of 5-star.</summary>
    public IReadOnlyList<int> ReviewRatingDistribution { get; set; } = new int[5];

    public int TotalConversations { get; set; }

    public int ConversationsThisMonth { get; set; }

    public int TotalMessages { get; set; }

    public int MessagesThisMonth { get; set; }

    /// <summary>Merged, time-sorted events across every module, platform-wide — capped at 15.</summary>
    public IReadOnlyList<RecentActivityItemDto> RecentActivity { get; set; } = Array.Empty<RecentActivityItemDto>();
}
