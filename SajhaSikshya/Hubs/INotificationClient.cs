using SajhaSikshya.DTOs.Notifications;

namespace SajhaSikshya.Hubs;

/// <summary>
/// The set of events a connected client can receive over <see cref="NotificationHub"/>,
/// as a strongly-typed <c>Hub&lt;INotificationClient&gt;</c> contract — same rationale
/// as Chat's <c>IChatClient</c> (compile-time-checked event names/arguments instead of
/// string-keyed <c>SendAsync</c> calls).
/// </summary>
public interface INotificationClient
{
    Task ReceiveNotification(NotificationDto notification);

    Task NotificationRead(int notificationId);

    Task NotificationDeleted(int notificationId);

    Task AllNotificationsRead();

    Task UnreadCountUpdated(int unreadCount);
}
