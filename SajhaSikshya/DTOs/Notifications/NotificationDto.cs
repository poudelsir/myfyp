using SajhaSikshya.Data.Enums;

namespace SajhaSikshya.DTOs.Notifications;

public class NotificationDto
{
    public int Id { get; set; }

    public NotificationType NotificationType { get; set; }

    public string NotificationTypeDisplay { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? Link { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ReadAtUtc { get; set; }
}
