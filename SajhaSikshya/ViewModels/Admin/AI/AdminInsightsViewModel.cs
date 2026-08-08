using SajhaSikshya.DTOs.AI;

namespace SajhaSikshya.ViewModels.Admin.AI;

/// <summary>Backs the Admin AI Insights dashboard. Only carries the raw stats — the AI summary loads separately via fetch() so a Gemini outage never blocks the page itself from rendering.</summary>
public class AdminInsightsViewModel
{
    public AdminInsightsStatsDto Stats { get; set; } = new();
}
