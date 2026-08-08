using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SajhaSikshya.Data.Constants;
using SajhaSikshya.DTOs.Chat;
using SajhaSikshya.Services.Interfaces.Chat;

namespace SajhaSikshya.Hubs;

/// <summary>
/// Real-time transport for Phase 7 Chat — separate from <see cref="NotificationHub"/>,
/// which is a generic per-user notification channel unrelated to chat's much richer,
/// conversation-scoped protocol (groups, typing, read receipts, message CRUD).
///
/// Every method re-validates the caller is an actual participant of the conversation
/// it's asked to act on, the same "never trust a caller-supplied id" discipline
/// <c>ChatService</c> already applies — a Hub method is exactly as reachable by a
/// forged direct invocation as a Controller action is by a forged HTTP request, so it
/// gets the same scrutiny. Non-participants get a <see cref="HubException"/> with the
/// same deliberately uninformative "Conversation not found." message the HTTP 404 path
/// uses, carrying the same "don't reveal whether the id exists" property across to a
/// transport that has no HTTP status codes to reuse literally.
///
/// Message send/edit/delete/mark-read all funnel through the exact same
/// <see cref="IChatService"/> methods the HTTP controller
/// (<c>Areas/Student/Controllers/ChatController</c>) already calls — <see cref="Chat.ChatService"/>
/// dispatches the resulting real-time event itself (via <see cref="IChatNotificationDispatcher"/>),
/// so it doesn't matter whether a mutation arrived over SignalR or a plain HTTP POST;
/// both paths broadcast identically, and neither the Hub nor the Controller contains
/// any business logic of its own.
///
/// Typing indicators are the one exception — they have no persistence and never touch
/// <see cref="IChatService"/>, so they're broadcast directly from here rather than
/// through the dispatcher (routing them through the dispatcher would mean Hub →
/// Dispatcher → Hub, a pointless round trip for a signal that never leaves the
/// real-time layer).
/// </summary>
[Authorize(Roles = Roles.Student)]
public class ChatHub : Hub<IChatClient>
{
    private readonly IChatService _chatService;
    private readonly IChatQueryService _chatQueryService;
    private readonly IChatPresenceTracker _presenceTracker;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(
        IChatService chatService,
        IChatQueryService chatQueryService,
        IChatPresenceTracker presenceTracker,
        ILogger<ChatHub> logger)
    {
        _chatService = chatService;
        _chatQueryService = chatQueryService;
        _presenceTracker = presenceTracker;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        await _presenceTracker.ConnectedAsync(UserId, Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await _presenceTracker.DisconnectedAsync(UserId, Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Adds this connection to the <c>conversation-{id}</c> group so it receives every
    /// real-time event for that conversation. Must be called again after a reconnect —
    /// SignalR group membership is per-connection, not per-user, and a reconnect gets a
    /// brand new connection id (the client's <c>onreconnected</c> handler re-invokes this).
    /// </summary>
    public async Task JoinConversation(int conversationId)
    {
        await EnsureParticipantAsync(conversationId);
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(conversationId));
    }

    public async Task LeaveConversation(int conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(conversationId));
    }

    public async Task SendMessage(int conversationId, string text)
    {
        await _chatService.SendMessageAsync(conversationId, UserId, text);
    }

    public async Task EditMessage(int conversationId, int messageId, string text)
    {
        _ = conversationId; // conversationId is only needed by the client to route the event; ChatService derives it from the message itself.
        await _chatService.EditMessageAsync(messageId, UserId, text);
    }

    public async Task DeleteMessage(int conversationId, int messageId)
    {
        _ = conversationId;
        await _chatService.DeleteMessageAsync(messageId, UserId);
    }

    public async Task MarkAsRead(int conversationId)
    {
        await _chatService.MarkMessagesAsReadAsync(conversationId, UserId);
    }

    /// <summary>Ephemeral, unpersisted — broadcast directly to the group, bypassing ChatService entirely (see class remarks).</summary>
    public async Task StartTyping(int conversationId)
    {
        var userName = await EnsureParticipantAsync(conversationId);
        await Clients.OthersInGroup(GroupName(conversationId)).TypingStarted(conversationId, UserId, userName);
    }

    public async Task StopTyping(int conversationId)
    {
        await EnsureParticipantAsync(conversationId);
        await Clients.OthersInGroup(GroupName(conversationId)).TypingStopped(conversationId, UserId);
    }

    /// <summary>Validates the caller is a participant and returns their own display name (used by typing events, which need it and would otherwise require a second lookup).</summary>
    private async Task<string> EnsureParticipantAsync(int conversationId)
    {
        var conversation = await _chatQueryService.GetConversationDetailsAsync(conversationId, UserId);
        if (conversation is null || (conversation.BuyerId != UserId && conversation.SellerId != UserId))
        {
            throw new HubException("Conversation not found.");
        }

        return conversation.BuyerId == UserId ? conversation.BuyerName : conversation.SellerName;
    }

    // Never actually null in practice — [Authorize(Roles = Roles.Student)] guarantees an
    // authenticated caller, and every such caller has the NameIdentifier claim the
    // default SignalR IUserIdProvider reads. Guarded anyway rather than trusting that.
    private string UserId => Context.UserIdentifier
        ?? throw new HubException("Not authenticated.");

    private static string GroupName(int conversationId) => $"conversation-{conversationId}";
}
