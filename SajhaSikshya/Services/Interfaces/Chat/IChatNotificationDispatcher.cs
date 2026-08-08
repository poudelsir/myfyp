using SajhaSikshya.DTOs.Chat;

namespace SajhaSikshya.Services.Interfaces.Chat;

/// <summary>
/// The one seam between chat's business logic and however a real-time event actually
/// reaches a client. <see cref="Chat.ChatService"/> depends only on this interface —
/// it has no idea SignalR exists. Business Services → NotificationDispatcher → SignalR
/// → (future) Email → (future) Push, exactly as the project's transport-abstraction
/// convention already works elsewhere (e.g. <c>IImageStorageService</c> hiding "public
/// wwwroot file" vs "private outside-wwwroot file" behind one interface). Today's only
/// implementation is <c>SignalRChatNotificationDispatcher</c>; a future phase adding
/// email digests or push notifications implements this same interface (or wraps it in
/// a composite that calls several) without touching <see cref="Chat.ChatService"/> at all.
///
/// Each method's payload is deliberately the minimum the event actually needs, not a
/// blanket "send the whole DTO everywhere": a new message needs the full
/// <see cref="MessageDto"/> (the recipient is rendering a brand-new bubble and needs
/// sender name/avatar), but an edit or delete only needs to patch an already-rendered
/// bubble, so those take just the changed fields.
/// </summary>
public interface IChatNotificationDispatcher
{
    /// <summary>Broadcasts a new message to every connection currently viewing this conversation (the <c>conversation-{id}</c> group — see <see cref="Hubs.ChatHub"/>).</summary>
    Task MessageSentAsync(MessageDto message);

    /// <summary>Broadcasts an in-place text update for an already-delivered message.</summary>
    Task MessageEditedAsync(int conversationId, int messageId, string newText, DateTime editedAtUtc);

    /// <summary>Broadcasts that a message was soft-deleted, so open clients can swap its bubble for the "deleted" placeholder without a full reload.</summary>
    Task MessageDeletedAsync(int conversationId, int messageId);

    /// <summary>Broadcasts that <paramref name="readerId"/> has read the conversation up to <paramref name="readAtUtc"/>, so the other participant's sent-message ticks can flip to "Read" live.</summary>
    Task ConversationReadAsync(int conversationId, string readerId, DateTime readAtUtc);

    /// <summary>
    /// Pushes <paramref name="userId"/>'s new total unread count to every one of THEIR
    /// OWN connections (all open tabs/devices) — targeted at the user directly rather
    /// than a conversation group, since unread count is a per-user, cross-conversation
    /// total, not something scoped to a single conversation's participants.
    /// </summary>
    Task UnreadCountChangedAsync(string userId, int unreadCount);
}
