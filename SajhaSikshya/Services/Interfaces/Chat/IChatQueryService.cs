using SajhaSikshya.DTOs;
using SajhaSikshya.DTOs.Chat;

namespace SajhaSikshya.Services.Interfaces.Chat;

/// <summary>
/// Read-only Chat queries, split out from <see cref="IChatService"/> the same way
/// <c>IOrderQueryService</c>/<c>IVerificationQueryService</c> are split from their
/// command counterparts. Every method returns DTOs, never tracked entities.
/// <see cref="GetConversationDetailsAsync"/> is deliberately unrestricted by
/// participant (like <c>IOrderQueryService.GetOrderDetailsAsync</c>) — the "buyer,
/// seller, or nobody" access rule is enforced by the controller after fetching, the
/// same pattern <c>VerificationImagesController</c> established.
/// </summary>
public interface IChatQueryService
{
    /// <summary>
    /// <paramref name="userId"/>'s own conversations (as buyer or seller), newest
    /// activity first, optionally narrowed by a search term matched against the other
    /// party's name or the listing title, and excluding conversations
    /// <paramref name="userId"/> has archived unless <paramref name="includeArchived"/>
    /// is set.
    /// </summary>
    Task<PagedResult<ConversationDto>> GetConversationsAsync(string userId, string? searchTerm, bool includeArchived, int pageNumber, int pageSize);

    /// <summary>Full detail lookup for one conversation, viewer-relative fields computed against <paramref name="requestingUserId"/>. Null if the id doesn't exist.</summary>
    Task<ConversationDto?> GetConversationDetailsAsync(int conversationId, string requestingUserId);

    /// <summary>
    /// One page of a conversation's messages, oldest-first within the page for natural
    /// reading order — but "page 1" always means the most recent messages (the
    /// underlying query orders newest-first before paging), so opening a conversation
    /// shows the latest activity and "load older" advances to page 2, 3, etc. Includes
    /// soft-deleted messages (rendered as placeholders) so the thread's flow is intact.
    /// </summary>
    Task<PagedResult<MessageDto>> GetMessagesAsync(int conversationId, int pageNumber, int pageSize);

    /// <summary>Total unread message count across every conversation <paramref name="userId"/> participates in — for a nav badge.</summary>
    Task<int> GetUnreadCountAsync(string userId);

    /// <summary>Every non-deleted Image/File message in a conversation, newest first — the conversation's "media" view (Phase 7.3).</summary>
    Task<PagedResult<MessageDto>> GetAttachmentsAsync(int conversationId, int pageNumber, int pageSize);

    /// <summary>
    /// The minimal fields needed to authorize and serve one attachment — see
    /// <see cref="ChatAttachmentAccessDto"/>. Deliberately unrestricted by participant
    /// (same pattern as <see cref="GetConversationDetailsAsync"/>); the controller
    /// checks buyer-or-seller after fetching. Null if the message doesn't exist, has no
    /// attachment, or has been deleted — all three collapse to "nothing to serve",
    /// which the controller turns into a 404 either way.
    /// </summary>
    Task<ChatAttachmentAccessDto?> GetAttachmentAccessAsync(int messageId);

    /// <summary>
    /// Message-level search across every conversation <paramref name="userId"/>
    /// participates in — matches message text, the listing title, or either party's
    /// name, newest first. Distinct from <see cref="GetConversationsAsync"/>'s own
    /// search (which also matches message text now, but still returns whole
    /// conversation rows) — this returns individual message hits with enough context
    /// to jump straight to where each match lives.
    /// </summary>
    Task<PagedResult<MessageSearchResultDto>> SearchMessagesAsync(string userId, string searchTerm, int pageNumber, int pageSize);
}
