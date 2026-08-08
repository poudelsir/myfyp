using SajhaSikshya.Data.Entities.Chat;
using SajhaSikshya.Data.Enums;
using SajhaSikshya.DTOs.Chat;
using SajhaSikshya.Extensions;

namespace SajhaSikshya.Mappings.Chat;

public static class ChatMappings
{
    /// <summary>
    /// Callers must Include Buyer/Seller/Listing.ThumbnailImage first — this only reads
    /// off already-loaded navigation properties, it doesn't query. <paramref name="latestMessage"/>
    /// and <paramref name="unreadCount"/> come from <c>ChatQueryService</c>'s own small
    /// per-conversation lookups (see its remarks on why those aren't projected inline).
    /// </summary>
    public static ConversationDto ToDto(this Conversation conversation, string viewerId, Message? latestMessage, int unreadCount)
    {
        var isBuyer = viewerId == conversation.BuyerId;

        return new ConversationDto
        {
            Id = conversation.Id,
            ListingId = conversation.ListingId,
            ListingTitle = conversation.Listing?.Title ?? string.Empty,
            ListingSlug = conversation.Listing?.Slug ?? string.Empty,
            ListingThumbnailImagePath = conversation.Listing?.ThumbnailImage?.ImagePath,
            BuyerId = conversation.BuyerId,
            BuyerName = conversation.Buyer?.FullName ?? string.Empty,
            BuyerProfilePicturePath = conversation.Buyer?.ProfilePicturePath,
            SellerId = conversation.SellerId,
            SellerName = conversation.Seller?.FullName ?? string.Empty,
            SellerProfilePicturePath = conversation.Seller?.ProfilePicturePath,
            OtherPartyName = isBuyer ? (conversation.Seller?.FullName ?? string.Empty) : (conversation.Buyer?.FullName ?? string.Empty),
            OtherPartyProfilePicturePath = isBuyer ? conversation.Seller?.ProfilePicturePath : conversation.Buyer?.ProfilePicturePath,
            CreatedAtUtc = conversation.CreatedAtUtc,
            LastMessageAtUtc = conversation.LastMessageAtUtc,
            LastMessagePreview = DescribeLatestMessage(latestMessage),
            LastMessageSenderId = latestMessage?.SenderId,
            IsArchived = isBuyer ? conversation.ArchivedByBuyer : conversation.ArchivedBySeller,
            UnreadCount = unreadCount,
        };
    }

    /// <summary>Callers must Include Sender first.</summary>
    public static MessageDto ToDto(this Message message)
    {
        return new MessageDto
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            SenderId = message.SenderId,
            SenderName = message.Sender?.FullName ?? string.Empty,
            SenderProfilePicturePath = message.Sender?.ProfilePicturePath,
            MessageType = message.MessageType,
            Text = message.IsDeleted ? null : message.Text,
            OriginalFileName = message.IsDeleted ? null : message.OriginalFileName,
            ContentType = message.IsDeleted ? null : message.ContentType,
            FileSizeBytes = message.IsDeleted ? null : message.FileSizeBytes,
            IsEdited = message.IsEdited,
            EditedAtUtc = message.EditedAtUtc,
            IsDeleted = message.IsDeleted,
            ReadAtUtc = message.ReadAtUtc,
            CreatedAtUtc = message.CreatedAtUtc,
        };
    }

    private static string? DescribeLatestMessage(Message? message)
    {
        if (message is null)
        {
            return null;
        }

        if (message.MessageType != MessageType.Text)
        {
            return message.OriginalFileName ?? message.MessageType.GetDisplayName();
        }

        var text = message.Text ?? string.Empty;
        return text.Length > 120 ? text[..117] + "..." : text;
    }
}
