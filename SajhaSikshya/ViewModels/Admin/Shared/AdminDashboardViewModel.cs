using SajhaSikshya.DTOs.Dashboard;

namespace SajhaSikshya.ViewModels.Admin.Shared;

/// <summary>Backs the Admin landing dashboard — a thin wrapper around <see cref="AdminDashboardStatsDto"/>, the same shape as <c>ViewModels.Admin.AI.AdminInsightsViewModel</c>.</summary>
public class AdminDashboardViewModel
{
    public AdminDashboardStatsDto Stats { get; set; } = new();
}
