using Microsoft.AspNetCore.SignalR;
using SajhaSikshya.DTOs.Notifications;
using SajhaSikshya.Hubs;
using SajhaSikshya.Services.Interfaces.Notifications;

namespace SajhaSikshya.Services.Notifications;

/// <summary>
/// Implements <see cref="INotificationDispatcher"/> over SignalR — the only class that
/// knows <see cref="NotificationHub"/> exists, the same "one adapter class knows the
/// transport" shape as Chat's <c>SignalRChatNotificationDispatcher</c>.
///
/// Every call is wrapped and never throws: callers (Orders, Verification, Listing
/// moderation, Reviews, User management, ...) always invoke this AFTER their own
/// database write has already committed — a transient SignalR/backplane hiccup here
/// must not turn an already-successful business transaction into a 500 for the end
/// user. A delivery failure just means the live push didn't arrive; the notification
/// row itself was already saved and the recipient will see it next time they poll/load.
/// </summary>
public class SignalRNotificationDispatcher : INotificationDispatcher
{
    private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;
    private readonly ILogger<SignalRNotificationDispatcher> _logger;

    public SignalRNotificationDispatcher(IHubContext<NotificationHub, INotificationClient> hubContext, ILogger<SignalRNotificationDispatcher> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public Task NotificationCreatedAsync(string userId, NotificationDto notification) =>
        SafeDispatchAsync(() => _hubContext.Clients.User(userId).ReceiveNotification(notification), nameof(NotificationCreatedAsync));

    public Task NotificationReadAsync(string userId, int notificationId) =>
        SafeDispatchAsync(() => _hubContext.Clients.User(userId).NotificationRead(notificationId), nameof(NotificationReadAsync));

    public Task NotificationDeletedAsync(string userId, int notificationId) =>
        SafeDispatchAsync(() => _hubContext.Clients.User(userId).NotificationDeleted(notificationId), nameof(NotificationDeletedAsync));

    public Task AllNotificationsReadAsync(string userId) =>
        SafeDispatchAsync(() => _hubContext.Clients.User(userId).AllNotificationsRead(), nameof(AllNotificationsReadAsync));

    public Task UnreadCountChangedAsync(string userId, int unreadCount) =>
        SafeDispatchAsync(() => _hubContext.Clients.User(userId).UnreadCountUpdated(unreadCount), nameof(UnreadCountChangedAsync));

    private async Task SafeDispatchAsync(Func<Task> send, string methodName)
    {
        try
        {
            await send();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR notification dispatch failed in {Method} — the underlying data change already committed; only the live push was lost.", methodName);
        }
    }
}
