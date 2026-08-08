namespace SajhaSikshya.DTOs.AI;

/// <summary>AI-generated narrative layer on top of <see cref="AdminInsightsStatsDto"/> — purely additive; the dashboard's summary cards/charts render fully without this.</summary>
public class AdminInsightsSummaryDto
{
    public string Summary { get; set; } = string.Empty;

    public IReadOnlyList<string> Insights { get; set; } = Array.Empty<string>();
}
