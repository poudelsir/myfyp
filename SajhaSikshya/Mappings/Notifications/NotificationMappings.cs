using SajhaSikshya.Data.Entities.Notifications;
using SajhaSikshya.DTOs.Notifications;
using SajhaSikshya.Extensions;

namespace SajhaSikshya.Mappings.Notifications;

public static class NotificationMappings
{
    public static NotificationDto ToDto(this Notification notification)
    {
        return new NotificationDto
        {
            Id = notification.Id,
            NotificationType = notification.NotificationType,
            NotificationTypeDisplay = notification.NotificationType.GetDisplayName(),
            Title = notification.Title,
            Message = notification.Message,
            Link = notification.Link,
            IsRead = notification.IsRead,
            CreatedAtUtc = notification.CreatedAtUtc,
            ReadAtUtc = notification.ReadAtUtc,
        };
    }

    public static NotificationPreferenceDto ToDto(this NotificationPreference preference)
    {
        return new NotificationPreferenceDto
        {
            ChatEnabled = preference.ChatEnabled,
            OrdersEnabled = preference.OrdersEnabled,
            VerificationEnabled = preference.VerificationEnabled,
            MarketplaceEnabled = preference.MarketplaceEnabled,
            AnnouncementsEnabled = preference.AnnouncementsEnabled,
            EmailEnabled = preference.EmailEnabled,
            PushEnabled = preference.PushEnabled,
        };
    }
}
