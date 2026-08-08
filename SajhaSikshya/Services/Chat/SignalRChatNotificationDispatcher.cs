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
/// </summary>
public class SignalRChatNotificationDispatcher : IChatNotificationDispatcher
{
    private readonly IHubContext<ChatHub, IChatClient> _hubContext;

    public SignalRChatNotificationDispatcher(IHubContext<ChatHub, IChatClient> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task MessageSentAsync(MessageDto message) =>
        _hubContext.Clients.Group(GroupName(message.ConversationId)).ReceiveMessage(message);

    public Task MessageEditedAsync(int conversationId, int messageId, string newText, DateTime editedAtUtc) =>
        _hubContext.Clients.Group(GroupName(conversationId)).MessageEdited(conversationId, messageId, newText, editedAtUtc);

    public Task MessageDeletedAsync(int conversationId, int messageId) =>
        _hubContext.Clients.Group(GroupName(conversationId)).MessageDeleted(conversationId, messageId);

    public Task ConversationReadAsync(int conversationId, string readerId, DateTime readAtUtc) =>
        _hubContext.Clients.Group(GroupName(conversationId)).ReadReceiptUpdated(conversationId, readerId, readAtUtc);

    public Task UnreadCountChangedAsync(string userId, int unreadCount) =>
        _hubContext.Clients.User(userId).UnreadCountUpdated(unreadCount);

    private static string GroupName(int conversationId) => $"conversation-{conversationId}";
}
