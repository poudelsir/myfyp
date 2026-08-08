using SajhaSikshya.DTOs;
using SajhaSikshya.DTOs.Chat;

namespace SajhaSikshya.ViewModels.Chat;

/// <summary>Backs the Chat "media" view — every image/document shared in one conversation.</summary>
public class ConversationAttachmentsViewModel
{
    public ConversationDto Conversation { get; set; } = null!;

    public PagedResult<MessageDto> Attachments { get; set; } = new();
}
