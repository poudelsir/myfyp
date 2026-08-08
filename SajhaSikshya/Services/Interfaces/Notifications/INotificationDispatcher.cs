using SajhaSikshya.DTOs.Notifications;

namespace SajhaSikshya.Services.Interfaces.Notifications;

/// <summary>
/// The seam between <see cref="Notifications.NotificationService"/> and however a
/// notification event actually reaches a client — the same "Business Service →
/// Dispatcher → SignalR → future Email/Push" abstraction <c>IChatNotificationDispatcher</c>
/// already established for Chat (see that interface's remarks). This is a genuinely
/// separate interface, not a literal merge with the Chat one: they push through
/// different Hubs (this one through <see cref="Hubs.NotificationHub"/>, Chat's through
/// <c>ChatHub</c>) to different client protocols (persisted Notification records here;
/// ephemeral typing/read-receipt/message events there), so collapsing them into one
/// interface would conflate two different domains behind a single abstraction for no
/// real benefit. What DOES generalize is the pattern itself — a business service that
/// knows nothing about SignalR, talking only to an interface — and this reapplies it.
/// Every method targets one specific user (<c>Clients.User(userId)</c>), never a group;
/// notifications are inherently per-user, with no "everyone watching this thing"
/// concept the way a chat conversation has participants.
/// </summary>
public interface INotificationDispatcher
{
    Task NotificationCreatedAsync(string userId, NotificationDto notification);

    Task NotificationReadAsync(string userId, int notificationId);

    Task NotificationDeletedAsync(string userId, int notificationId);

    Task AllNotificationsReadAsync(string userId);

    Task UnreadCountChangedAsync(string userId, int unreadCount);
}
