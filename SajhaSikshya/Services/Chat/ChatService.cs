using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SajhaSikshya.Constants;
using SajhaSikshya.Data.Entities.Chat;
using SajhaSikshya.Data.Entities.Marketplace;
using SajhaSikshya.Data.Enums;
using SajhaSikshya.Mappings.Chat;
using SajhaSikshya.Repositories.Interfaces;
using SajhaSikshya.Services.Interfaces;
using SajhaSikshya.Services.Interfaces.Chat;
using SajhaSikshya.Services.Interfaces.Notifications;
using SajhaSikshya.Services.Notifications;

namespace SajhaSikshya.Services.Chat;

/// <summary>
/// Implements <see cref="IChatService"/>. Every successful mutation notifies
/// <see cref="_dispatcher"/> after its own <see cref="IUnitOfWork.SaveChangesAsync"/>
/// commits — never before, so a real-time event can never fire for a change that then
/// fails to persist. This is the ONLY place real-time notifications are triggered from;
/// <see cref="Hubs.ChatHub"/> and <c>Areas/Student/Controllers/ChatController</c> both
/// call these same methods and get identical broadcast behavior for free, regardless
/// of which transport (SignalR or plain HTTP) a caller used.
///
/// A new message also creates a persisted, cross-module <see cref="Notifications.Notification"/>
/// for the recipient (Phase 8.1's pilot integration) via <see cref="_notificationService"/> —
/// deliberately separate from <see cref="_dispatcher"/>'s own chat-specific unread-count
/// push: that badge is scoped to the Chat feature itself (visible only while looking at
/// "Messages"), while the Notification Center's badge is the site-wide "you have
/// something to know about" signal, visible from anywhere. Both fire from the same
/// event; neither duplicates the other's job.
/// </summary>
public class ChatService : IChatService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IChatNotificationDispatcher _dispatcher;
    private readonly IImageStorageService _imageStorageService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        IUnitOfWork unitOfWork,
        IChatNotificationDispatcher dispatcher,
        IImageStorageService imageStorageService,
        INotificationService notificationService,
        ILogger<ChatService> logger)
    {
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
        _imageStorageService = imageStorageService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<ServiceResult<int>> CreateConversationAsync(string buyerId, int listingId)
    {
        var listingRepository = _unitOfWork.Repository<Listing>();
        var listing = await listingRepository.GetByIdAsync(listingId);
        if (listing is null)
        {
            return ServiceResult<int>.Failure("This listing no longer exists.");
        }

        if (listing.SellerId == buyerId)
        {
            return ServiceResult<int>.Failure("You cannot message yourself about your own listing.");
        }

        var conversationRepository = _unitOfWork.Repository<Conversation>();
        var existing = await conversationRepository.FirstOrDefaultAsync(c =>
            c.BuyerId == buyerId && c.SellerId == listing.SellerId && c.ListingId == listingId);

        if (existing is not null)
        {
            return ServiceResult<int>.Success(existing.Id);
        }

        var conversation = new Conversation
        {
            BuyerId = buyerId,
            SellerId = listing.SellerId,
            ListingId = listingId,
        };

        await conversationRepository.AddAsync(conversation);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Conversation {ConversationId} started for listing {ListingId} between buyer {BuyerId} and seller {SellerId}", conversation.Id, listingId, buyerId, listing.SellerId);

        return ServiceResult<int>.Success(conversation.Id);
    }

    public async Task<ServiceResult<int>> SendMessageAsync(int conversationId, string senderId, string text)
    {
        var trimmedText = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        if (trimmedText is null)
        {
            return ServiceResult<int>.Failure("Message cannot be empty.");
        }

        if (trimmedText.Length > ChatConstants.MaximumMessageLength)
        {
            return ServiceResult<int>.Failure($"Message cannot exceed {ChatConstants.MaximumMessageLength} characters.");
        }

        var conversation = await GetParticipantConversationAsync(conversationId, senderId);
        if (conversation is null)
        {
            return ServiceResult<int>.Failure("Conversation not found.");
        }

        var message = new Message
        {
            ConversationId = conversationId,
            SenderId = senderId,
            MessageType = MessageType.Text,
            Text = trimmedText,
        };

        return await PersistAndBroadcastMessageAsync(conversation, message);
    }

    public async Task<ServiceResult<int>> SendAttachmentAsync(int conversationId, string senderId, IFormFile file)
    {
        var conversation = await GetParticipantConversationAsync(conversationId, senderId);
        if (conversation is null)
        {
            return ServiceResult<int>.Failure("Conversation not found.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var messageType = ChatConstants.ImageAttachmentExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase)
            ? MessageType.Image
            : MessageType.File;

        var saveResult = await _imageStorageService.SavePrivateAsync(
            file,
            ChatConstants.AttachmentStorageSubfolder,
            ChatConstants.AllowedAttachmentExtensions,
            ChatConstants.MaximumAttachmentSizeMB,
            ChatConstants.AllowedAttachmentMimeTypes);

        if (!saveResult.Succeeded)
        {
            return ServiceResult<int>.Failure(saveResult.Errors.ToArray());
        }

        // IFormFile.OpenReadStream() returns a fresh, independently-readable stream each
        // call (ASP.NET Core buffers the upload to memory/disk during model binding) —
        // safe to read a second, separate time here even though SavePrivateAsync already
        // consumed its own stream above.
        string hash;
        await using (var hashStream = file.OpenReadStream())
        {
            hash = Convert.ToHexString(await SHA256.HashDataAsync(hashStream));
        }

        var message = new Message
        {
            ConversationId = conversationId,
            SenderId = senderId,
            MessageType = messageType,
            AttachmentPath = saveResult.Data,
            OriginalFileName = Path.GetFileName(file.FileName),
            StoredFileName = Path.GetFileName(saveResult.Data!),
            ContentType = file.ContentType,
            FileSizeBytes = file.Length,
            AttachmentHash = hash,
        };

        return await PersistAndBroadcastMessageAsync(conversation, message);
    }

    /// <summary>Loads the conversation and confirms <paramref name="userId"/> is actually one of its two parties — the one check every mutation on an existing conversation shares.</summary>
    private async Task<Conversation?> GetParticipantConversationAsync(int conversationId, string userId)
    {
        var conversation = await _unitOfWork.Repository<Conversation>().GetByIdAsync(conversationId);
        return conversation is not null && IsParticipant(conversation, userId) ? conversation : null;
    }

    /// <summary>
    /// Shared tail for both <see cref="SendMessageAsync"/> and <see cref="SendAttachmentAsync"/>:
    /// saves the message, stamps <see cref="Conversation.LastMessageAtUtc"/>, resurfaces
    /// the conversation for the recipient if they'd archived it, then broadcasts and
    /// pushes the recipient's updated unread count. <paramref name="message"/> must
    /// already have every content field set (Text, or the Attachment* fields) — this
    /// only handles the persistence/notification mechanics common to both message kinds.
    /// </summary>
    private async Task<ServiceResult<int>> PersistAndBroadcastMessageAsync(Conversation conversation, Message message)
    {
        var messageRepository = _unitOfWork.Repository<Message>();
        await messageRepository.AddAsync(message);

        conversation.LastMessageAtUtc = DateTime.UtcNow;

        // A new message should resurface the conversation for whoever it's going to —
        // otherwise the recipient would never see it if they'd previously archived the
        // thread. The sender's own archive state is untouched (irrelevant — they know
        // they're sending it).
        if (message.SenderId == conversation.BuyerId)
        {
            conversation.ArchivedBySeller = false;
        }
        else
        {
            conversation.ArchivedByBuyer = false;
        }

        _unitOfWork.Repository<Conversation>().Update(conversation);
        await _unitOfWork.SaveChangesAsync();

        // Reload with Sender included — the entity we just built only has SenderId, not
        // the navigation, and the real-time broadcast needs a fully-populated DTO so a
        // recipient can render a brand-new bubble (name/avatar) without a round trip.
        var savedMessage = await messageRepository.FirstOrDefaultAsync(m => m.Id == message.Id, q => q.Include(m => m.Sender));
        if (savedMessage is not null)
        {
            await _dispatcher.MessageSentAsync(savedMessage.ToDto());
        }

        var recipientId = message.SenderId == conversation.BuyerId ? conversation.SellerId : conversation.BuyerId;
        await _dispatcher.UnreadCountChangedAsync(recipientId, await GetUnreadCountForUserAsync(recipientId));

        var senderName = savedMessage?.Sender?.FullName ?? "Someone";
        var preview = message.MessageType == MessageType.Text
            ? message.Text ?? string.Empty
            : message.OriginalFileName ?? "an attachment";
        var (title, notificationMessage) = NotificationTemplates.NewChatMessage(senderName, preview);
        await _notificationService.CreateAsync(
            recipientId,
            NotificationType.Message,
            title,
            notificationMessage,
            link: $"/Student/Chat/Conversation/{conversation.Id}",
            createdBy: message.SenderId);

        return ServiceResult<int>.Success(message.Id);
    }

    public async Task<ServiceResult> EditMessageAsync(int messageId, string senderId, string newText)
    {
        if (string.IsNullOrWhiteSpace(newText))
        {
            return ServiceResult.Failure("Message cannot be empty.");
        }

        var trimmed = newText.Trim();
        if (trimmed.Length > ChatConstants.MaximumMessageLength)
        {
            return ServiceResult.Failure($"Message cannot exceed {ChatConstants.MaximumMessageLength} characters.");
        }

        var messageRepository = _unitOfWork.Repository<Message>();
        var message = await messageRepository.GetByIdAsync(messageId);
        if (message is null || message.SenderId != senderId)
        {
            return ServiceResult.Failure("Message not found.");
        }

        if (message.IsDeleted)
        {
            return ServiceResult.Failure("This message has been deleted.");
        }

        if (message.MessageType != MessageType.Text)
        {
            return ServiceResult.Failure("Only text messages can be edited.");
        }

        if (DateTime.UtcNow - message.CreatedAtUtc > TimeSpan.FromMinutes(ChatConstants.MessageEditWindowMinutes))
        {
            return ServiceResult.Failure($"Messages can only be edited within {ChatConstants.MessageEditWindowMinutes} minutes of sending.");
        }

        var editedAtUtc = DateTime.UtcNow;
        message.Text = trimmed;
        message.IsEdited = true;
        message.EditedAtUtc = editedAtUtc;
        message.UpdatedAtUtc = editedAtUtc;

        messageRepository.Update(message);
        await _unitOfWork.SaveChangesAsync();

        await _dispatcher.MessageEditedAsync(message.ConversationId, message.Id, trimmed, editedAtUtc);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeleteMessageAsync(int messageId, string senderId)
    {
        var messageRepository = _unitOfWork.Repository<Message>();
        var message = await messageRepository.GetByIdAsync(messageId);
        if (message is null || message.SenderId != senderId)
        {
            return ServiceResult.Failure("Message not found.");
        }

        if (message.IsDeleted)
        {
            return ServiceResult.Failure("This message is already deleted.");
        }

        message.IsDeleted = true;
        message.UpdatedAtUtc = DateTime.UtcNow;

        messageRepository.Update(message);
        await _unitOfWork.SaveChangesAsync();

        await _dispatcher.MessageDeletedAsync(message.ConversationId, message.Id);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> SetArchivedAsync(int conversationId, string userId, bool archived)
    {
        var conversation = await GetParticipantConversationAsync(conversationId, userId);
        if (conversation is null)
        {
            return ServiceResult.Failure("Conversation not found.");
        }

        if (userId == conversation.BuyerId)
        {
            conversation.ArchivedByBuyer = archived;
        }
        else
        {
            conversation.ArchivedBySeller = archived;
        }

        _unitOfWork.Repository<Conversation>().Update(conversation);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Success();
    }

    public async Task MarkMessagesAsReadAsync(int conversationId, string readerId)
    {
        var messageRepository = _unitOfWork.Repository<Message>();
        var unreadMessages = await messageRepository.FindAsync(m =>
            m.ConversationId == conversationId && m.SenderId != readerId && m.ReadAtUtc == null && !m.IsDeleted);

        if (unreadMessages.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var message in unreadMessages)
        {
            message.ReadAtUtc = now;
            messageRepository.Update(message);
        }

        await _unitOfWork.SaveChangesAsync();

        await _dispatcher.ConversationReadAsync(conversationId, readerId, now);
        await _dispatcher.UnreadCountChangedAsync(readerId, await GetUnreadCountForUserAsync(readerId));
    }

    /// <summary>
    /// Deliberately duplicates the one-line query <c>ChatQueryService.GetUnreadCountAsync</c>
    /// also runs, rather than having this command service depend on the query service
    /// (commands never depend on queries anywhere else in this project — see
    /// <c>OrderService</c>, which has the same shape) or extracting a shared internal
    /// helper class for a single COUNT query. A real duplicated abstraction would be a
    /// problem; a duplicated one-liner is cheaper than either alternative.
    /// </summary>
    private async Task<int> GetUnreadCountForUserAsync(string userId)
    {
        var messageRepository = _unitOfWork.Repository<Message>();
        return await messageRepository.CountAsync(m =>
            (m.Conversation.BuyerId == userId || m.Conversation.SellerId == userId)
            && m.SenderId != userId
            && m.ReadAtUtc == null
            && !m.IsDeleted);
    }

    private static bool IsParticipant(Conversation conversation, string userId) =>
        conversation.BuyerId == userId || conversation.SellerId == userId;
}
