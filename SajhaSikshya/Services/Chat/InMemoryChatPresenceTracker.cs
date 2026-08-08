using System.Collections.Concurrent;
using SajhaSikshya.Services.Interfaces.Chat;

namespace SajhaSikshya.Services.Chat;

/// <summary>
/// Implements <see cref="IChatPresenceTracker"/> with a process-local in-memory map —
/// correct for this single-instance deployment; a multi-instance deployment would need
/// a shared store (e.g. Redis) behind the same interface, which is exactly why this
/// sits behind an interface rather than being called directly. Registered as a
/// Singleton (see <c>ServiceCollectionExtensions</c>) since it must outlive any single
/// request/connection and be shared across every <see cref="Hubs.ChatHub"/> instance
/// (Hubs themselves are transient, one per invocation).
/// </summary>
public class InMemoryChatPresenceTracker : IChatPresenceTracker
{
    // One user can have several open connections (multiple tabs/devices) — a user is
    // "online" as long as this set is non-empty for them.
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _connectionsByUser = new();
    private readonly ConcurrentDictionary<string, DateTime> _lastSeenUtcByUser = new();

    public Task ConnectedAsync(string userId, string connectionId)
    {
        var connections = _connectionsByUser.GetOrAdd(userId, static _ => new ConcurrentDictionary<string, byte>());
        connections[connectionId] = 0;
        return Task.CompletedTask;
    }

    public Task DisconnectedAsync(string userId, string connectionId)
    {
        if (_connectionsByUser.TryGetValue(userId, out var connections))
        {
            connections.TryRemove(connectionId, out _);
            if (connections.IsEmpty)
            {
                _connectionsByUser.TryRemove(userId, out _);
                _lastSeenUtcByUser[userId] = DateTime.UtcNow;
            }
        }

        return Task.CompletedTask;
    }

    public Task<bool> IsOnlineAsync(string userId) =>
        Task.FromResult(_connectionsByUser.TryGetValue(userId, out var connections) && !connections.IsEmpty);

    public Task<DateTime?> GetLastSeenAsync(string userId) =>
        Task.FromResult(_lastSeenUtcByUser.TryGetValue(userId, out var lastSeen) ? (DateTime?)lastSeen : null);
}
