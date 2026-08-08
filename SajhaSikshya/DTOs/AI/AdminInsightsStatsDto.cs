namespace SajhaSikshya.DTOs.AI;

/// <summary>
/// Every raw, real fact behind the Admin AI Insights dashboard — populated entirely
/// from existing query services and small bounded repository counts (see
/// <see cref="Interfaces.AI.IAdminInsightsService.GetStatsAsync"/>), never from Gemini.
/// This is both what renders the dashboard's summary cards/charts (always available,
/// AI or no AI) and the only grounding <see cref="Prompts.AdminInsightsPromptBuilder"/>
/// is allowed to reason from — nothing in the AI summary can reference a number that
/// isn't a field on this type.
/// </summary>
public class AdminInsightsStatsDto
{
    // Marketplace / Listings
    public int TotalListings { get; set; }

    public int ActiveListings { get; set; }

    public int PendingListings { get; set; }

    public int DonationListings { get; set; }

    public int ListingsThisMonth { get; set; }

    public int ListingsLastMonth { get; set; }

    public int DonationsThisMonth { get; set; }

    public int DonationsLastMonth { get; set; }

    public IReadOnlyList<NameCountDto> TopCategories { get; set; } = Array.Empty<NameCountDto>();

    public IReadOnlyList<NameCountDto> TopUniversities { get; set; } = Array.Empty<NameCountDto>();

    // Orders
    public int TotalOrders { get; set; }

    public int CompletedOrders { get; set; }

    public int OrdersThisMonth { get; set; }

    public int OrdersLastMonth { get; set; }

    // Verification
    public int PendingVerifications { get; set; }

    public int ApprovedVerifications { get; set; }

    public int RejectedVerifications { get; set; }

    public double VerificationApprovalRatePercent { get; set; }

    // Reviews
    public int TotalReviews { get; set; }

    public double AverageRating { get; set; }

    public int ReportedReviews { get; set; }

    // Notifications
    public int TotalNotifications { get; set; }

    public int NotificationsThisMonth { get; set; }

    // AI usage
    public int TotalAICalls { get; set; }

    public int SuccessfulAICalls { get; set; }

    public int CacheHitCount { get; set; }

    public double AverageAIResponseTimeMs { get; set; }
}

/// <summary>A generic name+count pair — top categories, top universities, and any future breakdown share this shape rather than each getting a bespoke type.</summary>
public record NameCountDto(string Name, int Count);
