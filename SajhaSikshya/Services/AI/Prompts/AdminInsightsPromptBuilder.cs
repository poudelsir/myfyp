using System.Text;
using SajhaSikshya.DTOs.AI;

namespace SajhaSikshya.Services.AI.Prompts;

/// <summary>
/// Builds the prompt, response schema, and cache key for Admin AI Insights —
/// deliberately separate from the other three builders (Phase 10.4's "dedicated
/// AdminInsightsPromptBuilder" requirement), since this is the only feature reasoning
/// over a whole dashboard's worth of aggregate statistics rather than a single
/// listing or a conversational question. Every number that reaches the prompt comes
/// directly from <see cref="AdminInsightsStatsDto"/> (itself pure database
/// aggregation — see <see cref="IAdminInsightsService.GetStatsAsync"/>) with growth
/// percentages pre-computed here in C# rather than left for Gemini to calculate, so
/// the model narrates real numbers instead of doing — and possibly getting wrong —
/// arithmetic itself.
/// </summary>
public static class AdminInsightsPromptBuilder
{
    public const string PromptType = "AdminInsights";

    public static string Build(AdminInsightsStatsDto stats)
    {
        var sb = new StringBuilder();

        sb.AppendLine("""
            You are a data analyst producing a short briefing for a SajhaSikshya
            marketplace administrator. SajhaSikshya is a marketplace where Nepali
            university students buy, sell, and donate secondhand textbooks and study
            materials to each other.

            Use ONLY the numbers given below. Do not invent, estimate, or assume any
            figure that isn't explicitly provided. If a figure is zero or a list is
            empty, say so plainly rather than skipping it silently.
            """);

        sb.AppendLine();
        sb.AppendLine("Marketplace:");
        sb.AppendLine($"- Total listings: {stats.TotalListings} ({stats.ActiveListings} active, {stats.PendingListings} awaiting admin approval, {stats.DonationListings} are donations).");
        sb.AppendLine($"- New listings: {FormatGrowth(stats.ListingsThisMonth, stats.ListingsLastMonth)}.");
        sb.AppendLine($"- New donations: {FormatGrowth(stats.DonationsThisMonth, stats.DonationsLastMonth)}.");
        sb.AppendLine($"- Top categories by active listing count: {FormatTopList(stats.TopCategories)}.");
        sb.AppendLine($"- Top universities by active listing count: {FormatTopList(stats.TopUniversities)}.");

        sb.AppendLine();
        sb.AppendLine("Orders:");
        sb.AppendLine($"- Total orders: {stats.TotalOrders} ({stats.CompletedOrders} completed).");
        sb.AppendLine($"- New orders: {FormatGrowth(stats.OrdersThisMonth, stats.OrdersLastMonth)}.");

        sb.AppendLine();
        sb.AppendLine("Verification:");
        sb.AppendLine($"- {stats.PendingVerifications} pending, {stats.ApprovedVerifications} approved, {stats.RejectedVerifications} rejected. Approval rate: {stats.VerificationApprovalRatePercent:0.#}%.");

        sb.AppendLine();
        sb.AppendLine("Reviews:");
        sb.AppendLine($"- {stats.TotalReviews} total reviews, average rating {stats.AverageRating:0.0}/5, {stats.ReportedReviews} currently reported for moderation.");

        sb.AppendLine();
        sb.AppendLine("Notifications:");
        sb.AppendLine($"- {stats.TotalNotifications} total notifications sent ({stats.NotificationsThisMonth} this month).");

        sb.AppendLine();
        sb.AppendLine("AI feature usage:");
        var successRate = stats.TotalAICalls == 0 ? 0 : Math.Round(stats.SuccessfulAICalls * 100.0 / stats.TotalAICalls, 1);
        sb.AppendLine($"- {stats.TotalAICalls} total AI calls, {successRate:0.#}% successful, {stats.CacheHitCount} served from cache, average real response time {stats.AverageAIResponseTimeMs:0}ms.");

        sb.AppendLine();
        sb.AppendLine("""
            Provide:
            - summary: One or two plain-text sentences giving the overall picture.
            - insights: 3-6 short, specific bullet-point sentences, each grounded in one
              or more of the numbers above (e.g. growth/decline, a standout category or
              university, a notable approval or success rate). No markdown, no emojis,
              no numbers that weren't given to you.

            Respond with JSON matching the provided schema only.
            """);

        return sb.ToString();
    }

    public static object BuildResponseSchema() => new
    {
        type = "OBJECT",
        properties = new
        {
            summary = new { type = "STRING" },
            insights = new { type = "ARRAY", items = new { type = "STRING" } },
        },
        required = new[] { "summary", "insights" },
    };

    /// <summary>Built from the stats values themselves (not natural language) — a genuinely changed dashboard snapshot always misses the cache; an unchanged one within the TTL window doesn't re-spend a Gemini call on a Refresh click.</summary>
    public static string BuildCacheKey(AdminInsightsStatsDto stats) =>
        "ai:admininsights:" + string.Join("|", new object[]
        {
            stats.TotalListings, stats.ActiveListings, stats.PendingListings, stats.DonationListings,
            stats.ListingsThisMonth, stats.ListingsLastMonth, stats.DonationsThisMonth, stats.DonationsLastMonth,
            stats.TotalOrders, stats.CompletedOrders, stats.OrdersThisMonth, stats.OrdersLastMonth,
            stats.PendingVerifications, stats.ApprovedVerifications, stats.RejectedVerifications,
            stats.TotalReviews, stats.AverageRating, stats.ReportedReviews,
            stats.TotalNotifications, stats.NotificationsThisMonth,
            stats.TotalAICalls, stats.SuccessfulAICalls, stats.CacheHitCount,
            string.Join(",", stats.TopCategories.Select(c => $"{c.Name}:{c.Count}")),
            string.Join(",", stats.TopUniversities.Select(u => $"{u.Name}:{u.Count}")),
        });

    private static string FormatGrowth(int thisPeriod, int lastPeriod)
    {
        if (lastPeriod == 0)
        {
            return thisPeriod == 0
                ? "0 this month, 0 last month"
                : $"{thisPeriod} this month, 0 last month (no prior baseline to compare against)";
        }

        var changePercent = Math.Round((thisPeriod - lastPeriod) * 100.0 / lastPeriod, 0);
        var direction = changePercent >= 0 ? "+" : "";
        return $"{thisPeriod} this month vs {lastPeriod} last month ({direction}{changePercent:0}%)";
    }

    private static string FormatTopList(IReadOnlyList<NameCountDto> items)
    {
        if (items.Count == 0)
        {
            return "none recorded";
        }

        return string.Join(", ", items.Select(i => $"{i.Name} ({i.Count})"));
    }
}
