using System.ComponentModel.DataAnnotations;
using SajhaSikshya.Constants;
using SajhaSikshya.Data.Enums;

namespace SajhaSikshya.Data.Entities.Chat;

/// <summary>
/// A single message within a <see cref="Conversation"/>. Never physically removed —
/// <see cref="BaseEntity.IsDeleted"/> already satisfies the spec's "Message.IsDeleted"
/// field, and deliberately has NO query filter applied in
/// <see cref="Configurations.Chat.MessageConfiguration"/> (unlike every other entity in
/// this project) so a deleted message still occupies its slot in the thread — see that
/// configuration's remarks for why. <see cref="BaseEntity.CreatedAtUtc"/> doubles as the
/// spec's "Sent" timestamp for read-receipt tracking; <see cref="ReadAtUtc"/> is "Read".
/// "Delivered" is a live SignalR acknowledgment, not a persisted column — the spec's own
/// DB field list omits a DeliveredAtUtc column, so none is added here.
/// </summary>
public class Message : BaseEntity
{
    public int ConversationId { get; set; }

    public Conversation Conversation { get; set; } = null!;

    [Required]
    public string SenderId { get; set; } = string.Empty;

    public ApplicationUser Sender { get; set; } = null!;

    public MessageType MessageType { get; set; } = MessageType.Text;

    [StringLength(ChatConstants.MaximumMessageLength)]
    public string? Text { get; set; }

    /// <summary>Opaque private storage key returned by <see cref="Services.Interfaces.IImageStorageService.SavePrivateAsync"/> — resolved back to a physical file only through the authorized <c>ChatController.Attachment</c> endpoint. Null for plain text messages.</summary>
    [StringLength(300)]
    public string? AttachmentPath { get; set; }

    /// <summary>The uploader's own filename (e.g. "receipt.pdf") — shown in the UI and used as the download filename. Never used to build a filesystem path (see <see cref="AttachmentPath"/>/<see cref="StoredFileName"/>).</summary>
    [StringLength(255)]
    public string? OriginalFileName { get; set; }

    /// <summary>The generated, collision-safe filename actually written to disk (the tail segment of <see cref="AttachmentPath"/>) — stored explicitly rather than re-parsed from the path every time it's needed for display/audit.</summary>
    [StringLength(255)]
    public string? StoredFileName { get; set; }

    /// <summary>Validated MIME type at upload time, reused as-is when serving the file back (see <c>ChatController.Attachment</c>) rather than re-derived from the extension.</summary>
    [StringLength(100)]
    public string? ContentType { get; set; }

    public long? FileSizeBytes { get; set; }

    /// <summary>
    /// SHA256 of the uploaded file, hex-encoded. Not used by any logic in this phase —
    /// stored purely as forward preparation for future duplicate-detection or antivirus-
    /// rescanning features, the same "store now, no processing yet" pattern as
    /// <c>Order.PaymentMethod</c> from Phase 6.
    /// </summary>
    [StringLength(64)]
    public string? AttachmentHash { get; set; }

    public bool IsEdited { get; set; }

    public DateTime? EditedAtUtc { get; set; }

    /// <summary>Set once the recipient has opened the conversation past this message's send time — see <c>ChatService.MarkMessagesAsReadAsync</c>.</summary>
    public DateTime? ReadAtUtc { get; set; }
}
