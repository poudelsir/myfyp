using SajhaSikshya.DTOs.Dashboard;

namespace SajhaSikshya.ViewModels.Student.Dashboard;

/// <summary>Backs the Student landing dashboard — a thin wrapper around <see cref="StudentDashboardStatsDto"/>, mirroring <c>ViewModels.Admin.Shared.AdminDashboardViewModel</c>'s shape.</summary>
public class StudentDashboardViewModel
{
    public StudentDashboardStatsDto Stats { get; set; } = new();
}
