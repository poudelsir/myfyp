namespace SajhaSikshya.DTOs.Dashboard;

/// <summary>
/// A single normalized event for a dashboard's "Recent Activity" feed, merged in-memory
/// from several independent per-module bounded queries and sorted by
/// <see cref="TimestampUtc"/> — see <see cref="Services.Interfaces.Dashboard.IDashboardQueryService"/>'s
/// implementation for how each source is built.
/// </summary>
public record RecentActivityItemDto(
    string ActivityType,
    string Description,
    DateTime TimestampUtc,
    string Icon,
    string? Url
);
