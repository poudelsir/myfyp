using SajhaSikshya.DTOs;
using SajhaSikshya.DTOs.Chat;

namespace SajhaSikshya.ViewModels.Chat;

/// <summary>Backs the Chat conversation window — the thread header plus one page of messages.</summary>
public class ConversationViewModel
{
    public ConversationDto Conversation { get; set; } = null!;

    public PagedResult<MessageDto> Messages { get; set; } = new();

    /// <summary>
    /// The first (oldest, in reading order) message that was still unread by the
    /// current viewer at the moment this page load began — captured by the controller
    /// BEFORE it calls <c>IChatService.MarkMessagesAsReadAsync</c>, since that call
    /// would otherwise erase the very state the "New messages" separator needs to
    /// render. Null if nothing was unread.
    /// </summary>
    public int? FirstUnreadMessageId { get; set; }

    /// <summary>Best-effort, point-in-time snapshot from <c>IChatPresenceTracker</c> — not live-updated without a page reload (see Phase 7.2/7.3 notes on why presence intentionally has no broadcast yet).</summary>
    public bool IsOtherPartyOnline { get; set; }
}
