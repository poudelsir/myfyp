using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SajhaSikshya.Authorization;
using SajhaSikshya.Constants;
using SajhaSikshya.Data.Constants;
using SajhaSikshya.DTOs.Chat;
using SajhaSikshya.Extensions;
using SajhaSikshya.Helpers;
using SajhaSikshya.Services.Interfaces;
using SajhaSikshya.Services.Interfaces.Chat;
using SajhaSikshya.ViewModels.Chat;

namespace SajhaSikshya.Areas.Student.Controllers;

/// <summary>
/// A Student's own conversations — both buyer-initiated and seller-received threads
/// live in one controller, the same "one role, one controller" shape
/// <c>Areas/Student/Controllers/OrdersController</c> established. Mutations go through
/// <see cref="IChatService"/>; reads go through <see cref="IChatQueryService"/>.
/// <see cref="Start"/> is the one action gated by
/// <see cref="AuthorizationPolicies.VerifiedStudent"/>. Every other action checks the
/// caller is a party to the specific conversation, matching the security requirement
/// that a non-participant gets a 404, never a 403 — <see cref="Conversation"/> and
/// <see cref="Attachment"/> both return <see cref="NotFoundResult"/> rather than any
/// variant of "forbidden".
/// </summary>
[Area("Student")]
[Authorize(Roles = Roles.Student)]
public class ChatController : Controller
{
    private const int ConversationPageSize = PaginationConstants.DefaultPageSize;
    private const int MessagePageSize = 30;
    private const int AttachmentPageSize = 20;
    private const int SearchPageSize = 20;

    private readonly IChatService _chatService;
    private readonly IChatQueryService _chatQueryService;
    private readonly IImageStorageService _imageStorageService;
    private readonly IChatPresenceTracker _presenceTracker;

    public ChatController(
        IChatService chatService,
        IChatQueryService chatQueryService,
        IImageStorageService imageStorageService,
        IChatPresenceTracker presenceTracker)
    {
        _chatService = chatService;
        _chatQueryService = chatQueryService;
        _imageStorageService = imageStorageService;
        _presenceTracker = presenceTracker;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm, bool includeArchived = false, int pageNumber = 1)
    {
        var userId = User.GetUserId()!;
        var model = new ConversationListViewModel
        {
            Page = await _chatQueryService.GetConversationsAsync(userId, searchTerm, includeArchived, pageNumber, ConversationPageSize),
            SearchTerm = searchTerm,
            IncludeArchived = includeArchived,
        };

        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.VerifiedStudent)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(int listingId, string? returnUrl)
    {
        var buyerId = User.GetUserId()!;
        var result = await _chatService.CreateConversationAsync(buyerId, listingId);

        if (!result.Succeeded)
        {
            TempData[AlertHelper.ErrorKey] = result.Errors.FirstOrDefault();
            return RedirectBack(returnUrl);
        }

        return RedirectToAction(nameof(Conversation), new { id = result.Data });
    }

    [HttpGet]
    public async Task<IActionResult> Conversation(int id, int pageNumber = 1)
    {
        var userId = User.GetUserId()!;
        var conversation = await _chatQueryService.GetConversationDetailsAsync(id, userId);
        if (conversation is null || !IsParticipant(conversation))
        {
            return NotFound();
        }

        // Fetch messages BEFORE marking them read — the "New messages" separator needs
        // to know what was still unread the moment this page load began, and
        // MarkMessagesAsReadAsync below would otherwise erase that state first.
        var messages = await _chatQueryService.GetMessagesAsync(id, pageNumber, MessagePageSize);
        var firstUnreadMessageId = messages.Items.FirstOrDefault(m => m.SenderId != userId && !m.ReadAtUtc.HasValue)?.Id;

        await _chatService.MarkMessagesAsReadAsync(id, userId);

        var otherPartyId = userId == conversation.BuyerId ? conversation.SellerId : conversation.BuyerId;

        var model = new ConversationViewModel
        {
            Conversation = conversation,
            Messages = messages,
            FirstUnreadMessageId = firstUnreadMessageId,
            IsOtherPartyOnline = await _presenceTracker.IsOnlineAsync(otherPartyId),
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("write-actions")]
    public async Task<IActionResult> Send(int id, string text)
    {
        var result = await _chatService.SendMessageAsync(id, User.GetUserId()!, text);
        if (!result.Succeeded)
        {
            TempData[AlertHelper.ErrorKey] = result.Errors.FirstOrDefault();
        }

        return RedirectToAction(nameof(Conversation), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadAttachment(int id, IFormFile file)
    {
        var result = await _chatService.SendAttachmentAsync(id, User.GetUserId()!, file);
        if (!result.Succeeded)
        {
            TempData[AlertHelper.ErrorKey] = result.Errors.FirstOrDefault();
        }

        return RedirectToAction(nameof(Conversation), new { id });
    }

    /// <summary>
    /// The one authorized path to a chat attachment — never a public static-file URL
    /// (the file lives outside <c>wwwroot</c>; see <see cref="IImageStorageService.SavePrivateAsync"/>).
    /// <paramref name="download"/> controls Content-Disposition: omitted (the default)
    /// serves inline, for <c>&lt;img&gt;</c> thumbnails and the full-screen image
    /// preview; <c>true</c> forces a "Save As" download with the original filename, for
    /// the document download button.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Attachment(int messageId, bool download = false)
    {
        var access = await _chatQueryService.GetAttachmentAccessAsync(messageId);
        if (access is null || (User.GetUserId() != access.BuyerId && User.GetUserId() != access.SellerId))
        {
            return NotFound();
        }

        var physicalPath = _imageStorageService.GetPrivatePhysicalPath(access.AttachmentPath);
        if (physicalPath is null)
        {
            return NotFound();
        }

        return download
            ? PhysicalFile(physicalPath, access.ContentType, access.OriginalFileName)
            : PhysicalFile(physicalPath, access.ContentType);
    }

    [HttpGet]
    public async Task<IActionResult> Attachments(int id, int pageNumber = 1)
    {
        var userId = User.GetUserId()!;
        var conversation = await _chatQueryService.GetConversationDetailsAsync(id, userId);
        if (conversation is null || !IsParticipant(conversation))
        {
            return NotFound();
        }

        var model = new ConversationAttachmentsViewModel
        {
            Conversation = conversation,
            Attachments = await _chatQueryService.GetAttachmentsAsync(id, pageNumber, AttachmentPageSize),
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Search(string? searchTerm, int pageNumber = 1)
    {
        var model = new MessageSearchViewModel { SearchTerm = searchTerm };

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            model.Results = await _chatQueryService.SearchMessagesAsync(User.GetUserId()!, searchTerm, pageNumber, SearchPageSize);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, int messageId, string text)
    {
        var result = await _chatService.EditMessageAsync(messageId, User.GetUserId()!, text);
        TempData[result.Succeeded ? AlertHelper.SuccessKey : AlertHelper.ErrorKey] =
            result.Succeeded ? "Message updated." : result.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Conversation), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int messageId)
    {
        var result = await _chatService.DeleteMessageAsync(messageId, User.GetUserId()!);
        TempData[result.Succeeded ? AlertHelper.SuccessKey : AlertHelper.ErrorKey] =
            result.Succeeded ? "Message deleted." : result.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Conversation), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(int id, bool archived = true)
    {
        var result = await _chatService.SetArchivedAsync(id, User.GetUserId()!, archived);
        TempData[result.Succeeded ? AlertHelper.SuccessKey : AlertHelper.ErrorKey] =
            result.Succeeded ? (archived ? "Conversation archived." : "Conversation restored.") : result.Errors.FirstOrDefault();
        return archived ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Conversation), new { id });
    }

    private bool IsParticipant(ConversationDto conversation)
    {
        var userId = User.GetUserId();
        return userId == conversation.BuyerId || userId == conversation.SellerId;
    }

    private IActionResult RedirectBack(string? returnUrl)
    {
        return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Index));
    }
}
