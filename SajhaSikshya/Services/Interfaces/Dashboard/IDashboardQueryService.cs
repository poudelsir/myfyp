using SajhaSikshya.DTOs.Dashboard;

namespace SajhaSikshya.Services.Interfaces.Dashboard;

/// <summary>
/// Read-only aggregation across every module's own query service, for the Student and
/// Admin landing dashboards. One service (not two) because both methods are the same
/// shape of work — compose existing DTOs, add only the handful of bounded counts no
/// existing query service exposes, merge, return — the same "student-facing and
/// admin-facing reads side by side" convention <see cref="Orders.IOrderQueryService"/>/
/// <see cref="Verification.IVerificationQueryService"/> already use. Never mutates
/// anything; safe to call on every dashboard page load.
/// </summary>
public interface IDashboardQueryService
{
    /// <summary>Everything the Student landing dashboard needs, scoped to <paramref name="userId"/>.</summary>
    Task<StudentDashboardStatsDto> GetStudentDashboardAsync(string userId);

    /// <summary>
    /// Everything the Admin landing dashboard needs, platform-wide. Composes
    /// <see cref="AI.IAdminInsightsService.GetStatsAsync"/> for AI/listing-growth/
    /// verification numbers rather than recomputing them.
    /// </summary>
    Task<AdminDashboardStatsDto> GetAdminDashboardAsync();
}
