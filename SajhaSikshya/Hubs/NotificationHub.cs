using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SajhaSikshya.Hubs;

/// <summary>
/// Real-time notification channel. Each authenticated connection joins a group keyed
/// by their user id so server-side code can push a notification to one specific user
/// (see the notification bell placeholder in the dashboard layout). Domain-specific
/// notification events (new enrollment, graded assignment, etc.) are added here as
/// those features are built; this is intentionally just the transport for now.
/// </summary>
[Authorize]
public class NotificationHub : Hub
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
