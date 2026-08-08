using SajhaSikshya.DTOs.Chat;

namespace SajhaSikshya.Hubs;

/// <summary>
/// The set of events a connected chat client can receive, as a strongly-typed
/// <c>Hub&lt;IChatClient&gt;</c> contract (see <see cref="ChatHub"/>) instead of
/// string-keyed <c>Clients.Group(...).SendAsync("EventName", ...)</c> calls — a typo
/// in an event name or argument list becomes a compile error here instead of a
/// silent no-op discovered only by clicking around in a browser.
/// </summary>
public interface IChatClient
{
    Task ReceiveMessage(MessageDto message);

    Task MessageEdited(int conversationId, int messageId, string newText, DateTime editedAtUtc);

    Task MessageDeleted(int conversationId, int messageId);

    Task TypingStarted(int conversationId, string userId, string userName);

    Task TypingStopped(int conversationId, string userId);

    Task ReadReceiptUpdated(int conversationId, string readerId, DateTime readAtUtc);

    Task UnreadCountUpdated(int unreadCount);
}
