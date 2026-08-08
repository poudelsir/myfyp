namespace SajhaSikshya.Services.Interfaces.Chat;

/// <summary>
/// Lightweight online/offline/last-seen bookkeeping for chat connections — an
/// extension point only. Nothing in Phase 7.2 broadcasts presence changes or renders
/// them in the UI (the spec explicitly asked for infrastructure, not a full presence
/// feature); <see cref="Hubs.ChatHub"/> simply calls <see cref="ConnectedAsync"/>/
/// <see cref="DisconnectedAsync"/> on connect/disconnect so the data is already
/// correct and queryable whenever a future feature (a "last seen" badge, smarter
/// read-receipt timing, etc.) wants it, without needing to touch the Hub again.
/// </summary>
public interface IChatPresenceTracker
{
    Task ConnectedAsync(string userId, string connectionId);

    Task DisconnectedAsync(string userId, string connectionId);

    /// <summary>True if the user has at least one open connection right now.</summary>
    Task<bool> IsOnlineAsync(string userId);

    /// <summary>When the user's last connection closed, or null if they've never connected or are currently online.</summary>
    Task<DateTime?> GetLastSeenAsync(string userId);
}
