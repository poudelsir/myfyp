namespace SajhaSikshya.DTOs.Chat;

/// <summary>
/// One message-level search hit — distinct from browsing/searching the conversation
/// list itself (<see cref="ConversationDto"/>/<c>GetConversationsAsync</c>'s own search,
/// which matches listing title/party names/message text but still returns whole
/// conversations). This carries just enough conversation context to link straight back
/// to where the match lives.
/// </summary>
public class MessageSearchResultDto
{
    public int MessageId { get; set; }

    public int ConversationId { get; set; }

    public string ListingTitle { get; set; } = string.Empty;

    /// <summary>The other participant, relative to whoever ran the search — same "viewer-relative" convention as <see cref="ConversationDto.OtherPartyName"/>.</summary>
    public string OtherPartyName { get; set; } = string.Empty;

    public string SenderName { get; set; } = string.Empty;

    public string Snippet { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}
