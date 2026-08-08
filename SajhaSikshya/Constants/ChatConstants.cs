namespace SajhaSikshya.Constants;

/// <summary>Business-rule limits for the Chat module (Phase 7), mirroring how <see cref="OrderConstants"/> centralizes the Orders module's limits.</summary>
public static class ChatConstants
{
    public const int MaximumMessageLength = 2000;

    /// <summary>How long after sending a text message its sender may still edit it.</summary>
    public const int MessageEditWindowMinutes = 15;

    /// <summary>Subfolder under the private storage root (see <see cref="Services.Interfaces.IImageStorageService.SavePrivateAsync"/>) chat attachments are saved into.</summary>
    public const string AttachmentStorageSubfolder = "chat";

    public const int MaximumAttachmentSizeMB = 10;

    /// <summary>Extensions that classify an attachment as <see cref="Data.Enums.MessageType.Image"/> rather than <see cref="Data.Enums.MessageType.File"/> — used only to pick the right thumbnail/preview UI, not for validation (both sets are validated identically).</summary>
    public static readonly IReadOnlyList<string> ImageAttachmentExtensions = new[] { ".png", ".jpg", ".jpeg", ".webp" };

    public static readonly IReadOnlyList<string> DocumentAttachmentExtensions = new[] { ".pdf", ".docx", ".txt" };

    public static readonly IReadOnlyList<string> AllowedAttachmentExtensions = new[] { ".png", ".jpg", ".jpeg", ".webp", ".pdf", ".docx", ".txt" };

    public static readonly IReadOnlyList<string> AllowedAttachmentMimeTypes = new[]
    {
        "image/png",
        "image/jpeg",
        "image/webp",
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "text/plain",
    };
}
