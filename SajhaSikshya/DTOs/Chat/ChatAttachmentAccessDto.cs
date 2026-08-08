namespace SajhaSikshya.DTOs.Chat;

/// <summary>
/// The minimal fields needed to authorize and serve one message's attachment — deliberately
/// not a full <see cref="MessageDto"/> (this is never meant to reach a view or be
/// serialized to a client; only <see cref="Areas.Student.Controllers.ChatController.Attachment"/>
/// ever sees it). Same shape/purpose as <c>VerificationImageAccessDto</c>.
/// </summary>
public class ChatAttachmentAccessDto
{
    public string BuyerId { get; set; } = string.Empty;

    public string SellerId { get; set; } = string.Empty;

    public string AttachmentPath { get; set; } = string.Empty;

    public string OriginalFileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;
}
