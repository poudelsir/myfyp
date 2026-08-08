using SajhaSikshya.DTOs;
using SajhaSikshya.DTOs.Notifications;

namespace SajhaSikshya.ViewModels.Notifications;

/// <summary>Backs the Notification Center page — the full, paginated notification history.</summary>
public class NotificationCenterViewModel
{
    public PagedResult<NotificationDto> Page { get; set; } = new();
}
