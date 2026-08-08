using SajhaSikshya.Data.Enums;
using SajhaSikshya.DTOs.Notifications;

namespace SajhaSikshya.Services.Interfaces.Notifications;

/// <summary>
/// The single write path for notifications in the system. Every module (Orders,
/// Verification, Chat, Marketplace, and future Reviews/AI integrations) calls
/// <see cref="CreateAsync"/> instead of writing its own notification record directly —
/// the whole point of centralizing this service, per Phase 8's brief, is that no
/// business service ever touches the <c>Notifications</c> table itself. Split from
/// <see cref="INotificationQueryService"/> the same way every other module in this
/// project separates commands from queries.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Creates a notification for <paramref name="userId"/> and pushes it in real time
    /// (new notification + updated unread count) via the notification dispatcher —
    /// unless <paramref name="userId"/> has disabled notifications for
    /// <paramref name="notificationType"/>'s category (see
    /// <see cref="UpdatePreferencesAsync"/>), in which case this is a silent no-op that
    /// still reports success (a suppressed notification is not a failure of whatever
    /// business operation triggered it). <paramref name="createdBy"/> is the triggering
    /// actor's user id if there is one (e.g. a chat message's sender), or null for a
    /// system-generated notice.
    /// </summary>
    Task<ServiceResult<int>> CreateAsync(
        string userId,
        NotificationType notificationType,
        string title,
        string message,
        string? link = null,
        string? createdBy = null);

    /// <summary>
    /// Fans a notification out to many users at once — an Administrator broadcasting a
    /// System Announcement or Maintenance Notice. <paramref name="targetRole"/> null or
    /// empty means every active user; <see cref="Data.Constants.Roles.Student"/>/
    /// <see cref="Data.Constants.Roles.Admin"/> scopes it to just that role (the
    /// "Targeted Notifications" half of the spec — targeting by role rather than a
    /// hand-picked user list, which would need a much heavier user-picker UI for
    /// marginal benefit over "who this announcement is actually for"). Each recipient's
    /// own preference is still respected, exactly as <see cref="CreateAsync"/> would for
    /// a single recipient — this reuses the same per-recipient creation path internally,
    /// not a separate one. Returns how many users actually received it (after
    /// preference filtering) as <c>Data</c>.
    /// </summary>
    Task<ServiceResult<int>> CreateBroadcastAsync(
        NotificationType notificationType,
        string title,
        string message,
        string? link,
        string createdByUserId,
        string? targetRole = null);

    /// <summary>Marks one notification read. Fails unless the caller owns it. A no-op (still succeeds) if it's already read.</summary>
    Task<ServiceResult> MarkAsReadAsync(int notificationId, string userId);

    /// <summary>Marks every one of the caller's unread notifications read in a single batch update.</summary>
    Task<ServiceResult> MarkAllAsReadAsync(string userId);

    /// <summary>Soft-deletes one notification. Fails unless the caller owns it.</summary>
    Task<ServiceResult> DeleteAsync(int notificationId, string userId);

    /// <summary>Creates or updates the caller's notification preferences (upsert — most users have no row until their first save; see <see cref="Data.Entities.Notifications.NotificationPreference"/>).</summary>
    Task<ServiceResult> UpdatePreferencesAsync(string userId, NotificationPreferenceDto preferences);
}
