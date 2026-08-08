using SajhaSikshya.DTOs.AI;

namespace SajhaSikshya.Services.Interfaces.AI;

/// <summary>
/// Backs the Admin AI Insights dashboard. <see cref="GetStatsAsync"/> is pure database
/// aggregation — fast, no external dependency, always available — while
/// <see cref="GenerateSummaryAsync"/> is the one method that calls Gemini and can fail;
/// the two are deliberately separate so a Gemini outage can never take the dashboard's
/// real statistics down with it (Phase 10.4's "never break the dashboard").
/// </summary>
public interface IAdminInsightsService
{
    Task<AdminInsightsStatsDto> GetStatsAsync();

    Task<ServiceResult<AdminInsightsSummaryDto>> GenerateSummaryAsync(string adminUserId);
}
