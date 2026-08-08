using SajhaSikshya.DTOs;
using SajhaSikshya.DTOs.Notifications;

namespace SajhaSikshya.Services.Interfaces.Notifications;

/// <summary>
/// Read-only Notification queries, split out from <see cref="INotificationService"/>
/// the same way every other module's query service is split from its command
/// counterpart. Every method is scoped to the requesting user's own id — unlike
/// Orders/Verification/Chat, there is no "admin sees everyone's" variant here at all;
/// an Administrator's notifications are just as private as a Student's (see the
/// spec's "No global notification access").
/// </summary>
public interface INotificationQueryService
{
    /// <summary>Paginated notification history, newest first — backs both the Notification Center page and (page 1, small page size) the navbar dropdown's recent list.</summary>
    Task<PagedResult<NotificationDto>> GetHistoryAsync(string userId, int pageNumber, int pageSize);

    /// <summary>Total unread count — for the navbar bell badge.</summary>
    Task<int> GetUnreadCountAsync(string userId);

    /// <summary>The caller's notification preferences, or all-enabled defaults if they've never saved any (see <see cref="Data.Entities.Notifications.NotificationPreference"/>'s remarks — no row is created just by reading).</summary>
    Task<NotificationPreferenceDto> GetPreferencesAsync(string userId);
}
