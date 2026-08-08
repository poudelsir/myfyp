using Microsoft.AspNetCore.Http;

namespace SajhaSikshya.Services.Interfaces.Chat;

/// <summary>
/// Chat mutations (Phase 7.1). Split from <see cref="IChatQueryService"/> the same way
/// every other module in this project separates commands from queries. Verification
/// enforcement ("only verified students may start a conversation") is deliberately NOT
/// duplicated here — the controller's Start action carries
/// <c>[Authorize(Policy = AuthorizationPolicies.VerifiedStudent)]</c>, so by the time
/// <see cref="CreateConversationAsync"/> runs the caller is already known to be
/// verified, matching <c>IOrderService</c>'s identical reasoning. Participant checks
/// (only the buyer or seller may act on a given conversation/message), by contrast,
/// ARE enforced here, not just in the controller — every method below re-derives the
/// conversation's actual parties from the database rather than trusting a caller-
/// supplied id, so a forged request can never mutate a conversation the caller isn't
/// part of.
/// </summary>
public interface IChatService
{
    /// <summary>
    /// Starts a new conversation between <paramref name="buyerId"/> and the listing's
    /// seller, or returns the id of the existing one if this pair has already messaged
    /// about this listing — see the filtered unique index on <c>Conversation</c>. Fails
    /// if the listing doesn't exist or belongs to the requesting buyer.
    /// </summary>
    Task<ServiceResult<int>> CreateConversationAsync(string buyerId, int listingId);

    /// <summary>
    /// Sends a plain text message from <paramref name="senderId"/>, who must be a
    /// participant. Resurfaces the conversation for the recipient if they'd archived
    /// it, and stamps <see cref="Data.Entities.Chat.Conversation.LastMessageAtUtc"/> —
    /// the same persistence/broadcast tail <see cref="SendAttachmentAsync"/> shares.
    /// </summary>
    Task<ServiceResult<int>> SendMessageAsync(int conversationId, string senderId, string text);

    /// <summary>
    /// Sends an image/document attachment (Phase 7.3) — validates and saves
    /// <paramref name="file"/> via <see cref="Interfaces.IImageStorageService.SavePrivateAsync"/>
    /// (extension, MIME type, magic bytes, size, collision-safe filename) under
    /// <see cref="Constants.ChatConstants.AttachmentStorageSubfolder"/>, then persists and
    /// broadcasts the message through the exact same path <see cref="SendMessageAsync"/>
    /// uses. <see cref="Data.Enums.MessageType"/> is inferred from the file's extension
    /// (<see cref="Constants.ChatConstants.ImageAttachmentExtensions"/> vs.
    /// <see cref="Constants.ChatConstants.DocumentAttachmentExtensions"/>).
    /// </summary>
    Task<ServiceResult<int>> SendAttachmentAsync(int conversationId, string senderId, IFormFile file);

    /// <summary>Edits a text message's content. Fails unless the caller is its sender, it's a still-editable Text message, and it's within <see cref="Constants.ChatConstants.MessageEditWindowMinutes"/> of being sent.</summary>
    Task<ServiceResult> EditMessageAsync(int messageId, string senderId, string newText);

    /// <summary>Soft-deletes a message (see <c>MessageConfiguration</c>'s remarks on why deleted messages stay visible as placeholders). Fails unless the caller is its sender.</summary>
    Task<ServiceResult> DeleteMessageAsync(int messageId, string senderId);

    /// <summary>Archives or restores the conversation for whichever participant <paramref name="userId"/> is — independent of the other participant's own archive state.</summary>
    Task<ServiceResult> SetArchivedAsync(int conversationId, string userId, bool archived);

    /// <summary>Stamps <see cref="Data.Entities.Chat.Message.ReadAtUtc"/> on every unread message the other participant sent, as of the moment <paramref name="readerId"/> opens the conversation.</summary>
    Task MarkMessagesAsReadAsync(int conversationId, string readerId);
}
