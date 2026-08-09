using Microsoft.AspNetCore.SignalR;
using SajhaSikshya.DTOs.Chat;
using SajhaSikshya.Hubs;
using SajhaSikshya.Services.Interfaces.Chat;

namespace SajhaSikshya.Services.Chat;

/// <summary>
/// Implements <see cref="IChatNotificationDispatcher"/> over SignalR — the only class
/// in the Chat module that knows <see cref="ChatHub"/> exists. <see cref="Chat.ChatService"/>
/// depends on the interface, not this class, so a future email/push implementation (or
/// a composite that fans out to several) slots in without touching ChatService at all.
///
/// Every call is wrapped and never throws — same reasoning as
/// <see cref="Notifications.SignalRNotificationDispatcher"/>: the message/edit/delete
/// has already been persisted by <see cref="Chat.ChatService"/> before this runs, so a
/// transient SignalR delivery failure must not surface as a failed send/edit/delete to
/// the user who already succeeded.
/// </summary>
public class SignalRChatNotificationDispatcher : IChatNotificationDispatcher
{
    private readonly IHubContext<ChatHub, IChatClient> _hubContext;
    private readonly ILogger<SignalRChatNotificationDispatcher> _logger;

    public SignalRChatNotificationDispatcher(IHubContext<ChatHub, IChatClient> hubContext, ILogger<SignalRChatNotificationDispatcher> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public Task MessageSentAsync(MessageDto message) =>
        SafeDispatchAsync(() => _hubContext.Clients.Group(GroupName(message.ConversationId)).ReceiveMessage(message), nameof(MessageSentAsync));

    public Task MessageEditedAsync(int conversationId, int messageId, string newText, DateTime editedAtUtc) =>
        SafeDispatchAsync(() => _hubContext.Clients.Group(GroupName(conversationId)).MessageEdited(conversationId, messageId, newText, editedAtUtc), nameof(MessageEditedAsync));

    public Task MessageDeletedAsync(int conversationId, int messageId) =>
        SafeDispatchAsync(() => _hubContext.Clients.Group(GroupName(conversationId)).MessageDeleted(conversationId, messageId), nameof(MessageDeletedAsync));

    public Task ConversationReadAsync(int conversationId, string readerId, DateTime readAtUtc) =>
        SafeDispatchAsync(() => _hubContext.Clients.Group(GroupName(conversationId)).ReadReceiptUpdated(conversationId, readerId, readAtUtc), nameof(ConversationReadAsync));

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
            _logger.LogWarning(ex, "SignalR chat dispatch failed in {Method} — the underlying message change already committed; only the live push was lost.", methodName);
        }
    }

    private static string GroupName(int conversationId) => $"conversation-{conversationId}";
}
