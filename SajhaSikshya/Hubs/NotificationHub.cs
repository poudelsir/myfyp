using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SajhaSikshya.Hubs;

/// <summary>
/// Real-time notification channel (Phase 8) — now strongly typed via
/// <see cref="INotificationClient"/> instead of the original untyped <c>Hub</c>. Every
/// module's notifications (Orders, Verification, Chat, and future Reviews/AI/Admin
/// integrations) push through this one Hub via <see cref="Services.Interfaces.Notifications.INotificationDispatcher"/>,
/// which addresses connections with <c>Clients.User(userId)</c> — SignalR's default
/// <c>IUserIdProvider</c> reads the same claim <see cref="Context.UserIdentifier"/>
/// does below, so the explicit per-user group join here is redundant with that
/// built-in targeting (proven working this same way for Chat's unread-count push in
/// Phase 7.2) but kept as-is since it's harmless and this Hub predates that
/// discovery — not worth touching for zero behavioral gain.
/// </summary>
[Authorize]
public class NotificationHub : Hub<INotificationClient>
{
    public override async Task OnConnectedAsync()
    {
        if (Context.UserIdentifier is not null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, Context.UserIdentifier);
        }

        await base.OnConnectedAsync();
    }
}
