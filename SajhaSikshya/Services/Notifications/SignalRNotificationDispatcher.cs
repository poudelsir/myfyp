using Microsoft.AspNetCore.SignalR;
using SajhaSikshya.DTOs.Notifications;
using SajhaSikshya.Hubs;
using SajhaSikshya.Services.Interfaces.Notifications;

namespace SajhaSikshya.Services.Notifications;

/// <summary>
/// Implements <see cref="INotificationDispatcher"/> over SignalR — the only class that
/// knows <see cref="NotificationHub"/> exists, the same "one adapter class knows the
/// transport" shape as Chat's <c>SignalRChatNotificationDispatcher</c>.
/// </summary>
public class SignalRNotificationDispatcher : INotificationDispatcher
{
    private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;

    public SignalRNotificationDispatcher(IHubContext<NotificationHub, INotificationClient> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotificationCreatedAsync(string userId, NotificationDto notification) =>
        _hubContext.Clients.User(userId).ReceiveNotification(notification);

    public Task NotificationReadAsync(string userId, int notificationId) =>
        _hubContext.Clients.User(userId).NotificationRead(notificationId);

    public Task NotificationDeletedAsync(string userId, int notificationId) =>
        _hubContext.Clients.User(userId).NotificationDeleted(notificationId);

    public Task AllNotificationsReadAsync(string userId) =>
        _hubContext.Clients.User(userId).AllNotificationsRead();

    public Task UnreadCountChangedAsync(string userId, int unreadCount) =>
        _hubContext.Clients.User(userId).UnreadCountUpdated(unreadCount);
}
