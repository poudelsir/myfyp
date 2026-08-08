using SajhaSikshya.Data.Enums;

namespace SajhaSikshya.DTOs.Chat;

public class MessageDto
{
    public int Id { get; set; }

    public int ConversationId { get; set; }

    public string SenderId { get; set; } = string.Empty;

    public string SenderName { get; set; } = string.Empty;

    public string? SenderProfilePicturePath { get; set; }

    public MessageType MessageType { get; set; }

    /// <summary>Null once <see cref="IsDeleted"/> is true — <see cref="Mappings.Chat.ChatMappings.ToDto"/> deliberately blanks the content rather than exposing it, even though the underlying row is only soft-deleted.</summary>
    public string? Text { get; set; }

    /// <summary>
    /// The attachment's original filename, for display and as the "has an attachment"
    /// signal (check <c>OriginalFileName is not null</c> rather than a separate flag).
    /// Deliberately no raw storage path/key here — <c>Message.AttachmentPath</c> never
    /// leaves the server; the UI fetches/downloads via <c>ChatController.Attachment</c>,
    /// addressed by this message's own <see cref="Id"/>.
    /// </summary>
    public string? OriginalFileName { get; set; }

    public string? ContentType { get; set; }

    public long? FileSizeBytes { get; set; }

    public bool IsEdited { get; set; }

    public DateTime? EditedAtUtc { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? ReadAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
