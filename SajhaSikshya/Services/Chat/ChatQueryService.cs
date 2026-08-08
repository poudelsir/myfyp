using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SajhaSikshya.Data.Entities.Chat;
using SajhaSikshya.Data.Enums;
using SajhaSikshya.DTOs;
using SajhaSikshya.DTOs.Chat;
using SajhaSikshya.Extensions;
using SajhaSikshya.Mappings.Chat;
using SajhaSikshya.Repositories.Interfaces;
using SajhaSikshya.Services.Interfaces.Chat;

namespace SajhaSikshya.Services.Chat;

public class ChatQueryService : IChatQueryService
{
    private readonly IUnitOfWork _unitOfWork;

    public ChatQueryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<ConversationDto>> GetConversationsAsync(string userId, string? searchTerm, bool includeArchived, int pageNumber, int pageSize)
    {
        var repository = _unitOfWork.Repository<Conversation>();

        // Precomputed outside the expression tree — same safe pattern already used by
        // OrderQueryService.GetAdminOrdersAsync/VerificationQueryService.GetQueueAsync.
        var hasSearchTerm = !string.IsNullOrWhiteSpace(searchTerm);

        Expression<Func<Conversation, bool>> filter = c =>
            (c.BuyerId == userId || c.SellerId == userId)
            && (includeArchived
                || (c.BuyerId == userId && !c.ArchivedByBuyer)
                || (c.SellerId == userId && !c.ArchivedBySeller))
            && (!hasSearchTerm
                || c.Listing.Title.Contains(searchTerm!)
                || (c.Buyer.FirstName + " " + c.Buyer.LastName).Contains(searchTerm!)
                || (c.Seller.FirstName + " " + c.Seller.LastName).Contains(searchTerm!)
                || c.Messages.Any(m => !m.IsDeleted && m.MessageType == MessageType.Text && m.Text != null && m.Text.Contains(searchTerm!)));

        var page = await repository.GetPagedAsync(
            pageNumber,
            pageSize,
            filter: filter,
            orderBy: q => q.OrderByDescending(c => c.LastMessageAtUtc ?? c.CreatedAtUtc),
            include: IncludeConversationSummary);

        var items = new List<ConversationDto>(page.Items.Count);
        foreach (var conversation in page.Items)
        {
            items.Add(await ToDtoWithMessageSummaryAsync(conversation, userId));
        }

        return new PagedResult<ConversationDto>
        {
            Items = items,
            PageNumber = page.PageNumber,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
        };
    }

    public async Task<ConversationDto?> GetConversationDetailsAsync(int conversationId, string requestingUserId)
    {
        var repository = _unitOfWork.Repository<Conversation>();
        var conversation = await repository.FirstOrDefaultAsync(c => c.Id == conversationId, IncludeConversationSummary);
        return conversation is null ? null : await ToDtoWithMessageSummaryAsync(conversation, requestingUserId);
    }

    public async Task<PagedResult<MessageDto>> GetMessagesAsync(int conversationId, int pageNumber, int pageSize)
    {
        var repository = _unitOfWork.Repository<Message>();

        // No `!IsDeleted` filter — deleted messages stay in the thread as placeholders
        // (see MessageConfiguration's remarks); ChatMappings.ToDto blanks their content.
        var page = await repository.GetPagedAsync(
            pageNumber,
            pageSize,
            filter: m => m.ConversationId == conversationId,
            orderBy: q => q.OrderByDescending(m => m.CreatedAtUtc),
            include: q => q.Include(m => m.Sender));

        return new PagedResult<MessageDto>
        {
            // The query fetches newest-first so "page 1" always means "the latest
            // messages"; reversed here to ascending order for natural top-to-bottom
            // reading within the page.
            Items = page.Items.Select(m => m.ToDto()).Reverse().ToList(),
            PageNumber = page.PageNumber,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
        };
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        var messageRepository = _unitOfWork.Repository<Message>();

        // Message.Conversation lets this translate to a single SQL join instead of a
        // two-round-trip "fetch my conversation ids, then count" approach.
        return await messageRepository.CountAsync(m =>
            (m.Conversation.BuyerId == userId || m.Conversation.SellerId == userId)
            && m.SenderId != userId
            && m.ReadAtUtc == null
            && !m.IsDeleted);
    }

    public async Task<PagedResult<MessageDto>> GetAttachmentsAsync(int conversationId, int pageNumber, int pageSize)
    {
        var repository = _unitOfWork.Repository<Message>();
        var page = await repository.GetPagedAsync(
            pageNumber,
            pageSize,
            filter: m => m.ConversationId == conversationId
                && !m.IsDeleted
                && (m.MessageType == MessageType.Image || m.MessageType == MessageType.File),
            orderBy: q => q.OrderByDescending(m => m.CreatedAtUtc),
            include: q => q.Include(m => m.Sender));

        return new PagedResult<MessageDto>
        {
            Items = page.Items.Select(m => m.ToDto()).ToList(),
            PageNumber = page.PageNumber,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
        };
    }

    public async Task<ChatAttachmentAccessDto?> GetAttachmentAccessAsync(int messageId)
    {
        var repository = _unitOfWork.Repository<Message>();

        // Projects straight to the fields needed for the authorization check and file
        // lookup — never loads the full entity just to read a path (same pattern as
        // VerificationQueryService.GetImageAccessAsync).
        var results = await repository.FindProjectedAsync(
            filter: m => m.Id == messageId && !m.IsDeleted && m.AttachmentPath != null,
            orderBy: q => q.OrderBy(m => m.Id),
            take: 1,
            selector: m => new ChatAttachmentAccessDto
            {
                BuyerId = m.Conversation.BuyerId,
                SellerId = m.Conversation.SellerId,
                AttachmentPath = m.AttachmentPath!,
                OriginalFileName = m.OriginalFileName ?? "attachment",
                ContentType = m.ContentType ?? "application/octet-stream",
            });

        return results.FirstOrDefault();
    }

    public async Task<PagedResult<MessageSearchResultDto>> SearchMessagesAsync(string userId, string searchTerm, int pageNumber, int pageSize)
    {
        var repository = _unitOfWork.Repository<Message>();

        // Matches the message's own text, or the conversation it lives in by listing
        // title/either party's name — a search for "Alice" surfaces every message in a
        // conversation with Alice, not just messages that literally say "Alice".
        Expression<Func<Message, bool>> filter = m =>
            !m.IsDeleted
            && (m.Conversation.BuyerId == userId || m.Conversation.SellerId == userId)
            && ((m.MessageType == MessageType.Text && m.Text != null && m.Text.Contains(searchTerm))
                || m.Conversation.Listing.Title.Contains(searchTerm)
                || (m.Conversation.Buyer.FirstName + " " + m.Conversation.Buyer.LastName).Contains(searchTerm)
                || (m.Conversation.Seller.FirstName + " " + m.Conversation.Seller.LastName).Contains(searchTerm));

        var page = await repository.GetPagedProjectedAsync(
            pageNumber,
            pageSize,
            filter: filter,
            orderBy: q => q.OrderByDescending(m => m.CreatedAtUtc),
            selector: m => new
            {
                m.Id,
                m.ConversationId,
                m.Text,
                m.MessageType,
                m.CreatedAtUtc,
                BuyerId = m.Conversation.BuyerId,
                ListingTitle = m.Conversation.Listing.Title,
                SenderName = m.Sender.FirstName + " " + m.Sender.LastName,
                BuyerName = m.Conversation.Buyer.FirstName + " " + m.Conversation.Buyer.LastName,
                SellerName = m.Conversation.Seller.FirstName + " " + m.Conversation.Seller.LastName,
            });

        var items = page.Items.Select(x => new MessageSearchResultDto
        {
            MessageId = x.Id,
            ConversationId = x.ConversationId,
            ListingTitle = x.ListingTitle,
            OtherPartyName = x.BuyerId == userId ? x.SellerName : x.BuyerName,
            SenderName = x.SenderName,
            Snippet = BuildSnippet(x.Text, x.MessageType),
            CreatedAtUtc = x.CreatedAtUtc,
        }).ToList();

        return new PagedResult<MessageSearchResultDto>
        {
            Items = items,
            PageNumber = page.PageNumber,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
        };
    }

    /// <summary>Runs after materialization (LINQ-to-Objects on already-fetched rows), so unlike the SQL-translated <c>selector</c> above, calling <c>.GetDisplayName()</c> here is safe.</summary>
    private static string BuildSnippet(string? text, MessageType messageType)
    {
        if (messageType != MessageType.Text)
        {
            return messageType.GetDisplayName();
        }

        var value = text ?? string.Empty;
        return value.Length > 150 ? value[..147] + "..." : value;
    }

    /// <summary>
    /// Computes the two per-conversation values GetPagedAsync's entity-level include
    /// can't project cheaply (the newest non-deleted message, and the viewer's own
    /// unread count) via two small, indexed queries bounded to a single conversation —
    /// never more than <c>pageSize</c> extra round-trips per page load, the same
    /// "several small bounded queries" tradeoff <c>OrderQueryService.GetStatisticsAsync</c>
    /// already accepts. If this becomes a real hot path at scale, the natural next step
    /// is denormalizing a last-message preview onto <see cref="Conversation"/> itself —
    /// deliberately not done in this phase since it's not part of the given schema and
    /// isn't yet needed.
    /// </summary>
    private async Task<ConversationDto> ToDtoWithMessageSummaryAsync(Conversation conversation, string viewerId)
    {
        var messageRepository = _unitOfWork.Repository<Message>();

        var latestPage = await messageRepository.GetPagedAsync(
            1,
            1,
            filter: m => m.ConversationId == conversation.Id && !m.IsDeleted,
            orderBy: q => q.OrderByDescending(m => m.CreatedAtUtc));
        var latestMessage = latestPage.Items.FirstOrDefault();

        var unreadCount = await messageRepository.CountAsync(m =>
            m.ConversationId == conversation.Id && m.SenderId != viewerId && m.ReadAtUtc == null && !m.IsDeleted);

        return conversation.ToDto(viewerId, latestMessage, unreadCount);
    }

    /// <summary>Enough to render a conversation row/header — buyer, seller, and the listing's title/thumbnail.</summary>
    private static IQueryable<Conversation> IncludeConversationSummary(IQueryable<Conversation> query) => query
        .Include(c => c.Buyer)
        .Include(c => c.Seller)
        .Include(c => c.Listing)
        .ThenInclude(l => l.ThumbnailImage);
}
