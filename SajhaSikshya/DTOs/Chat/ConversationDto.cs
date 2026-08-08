namespace SajhaSikshya.DTOs.Chat;

/// <summary>
/// A single conversation row, shaped for both the conversation list and the
/// conversation window's header — unlike Order's Dto/DetailDto split, there's no
/// meaningfully "thin" projection worth separating out here. Several fields
/// (<see cref="IsArchived"/>, <see cref="UnreadCount"/>, <see cref="OtherPartyName"/>)
/// are viewer-relative, computed by <see cref="Mappings.Chat.ChatMappings.ToDto"/>
/// against whichever participant requested it.
/// </summary>
public class ConversationDto
{
    public int Id { get; set; }

    public int ListingId { get; set; }

    public string ListingTitle { get; set; } = string.Empty;

    public string ListingSlug { get; set; } = string.Empty;

    public string? ListingThumbnailImagePath { get; set; }

    public string BuyerId { get; set; } = string.Empty;

    public string BuyerName { get; set; } = string.Empty;

    public string? BuyerProfilePicturePath { get; set; }

    public string SellerId { get; set; } = string.Empty;

    public string SellerName { get; set; } = string.Empty;

    public string? SellerProfilePicturePath { get; set; }

    /// <summary>The participant who is NOT the current viewer — whichever of Buyer/Seller that is. Saves every view from repeating the same ternary.</summary>
    public string OtherPartyName { get; set; } = string.Empty;

    public string? OtherPartyProfilePicturePath { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? LastMessageAtUtc { get; set; }

    /// <summary>Truncated text of the most recent non-deleted message, or a type label (e.g. "Image") for a non-text one. Null if no messages have been sent yet.</summary>
    public string? LastMessagePreview { get; set; }

    public string? LastMessageSenderId { get; set; }

    /// <summary>Whether the current viewer has archived this conversation (their own side of <see cref="Data.Entities.Chat.Conversation.ArchivedByBuyer"/>/<see cref="Data.Entities.Chat.Conversation.ArchivedBySeller"/>).</summary>
    public bool IsArchived { get; set; }

    /// <summary>Unread message count for the current viewer specifically.</summary>
    public int UnreadCount { get; set; }
}
