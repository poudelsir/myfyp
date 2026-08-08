namespace SajhaSikshya.DTOs.Notifications;

/// <summary>Mirrors <see cref="Data.Entities.Notifications.NotificationPreference"/> field-for-field; the boolean defaults here matter — they're what a user who has never saved a preference row is treated as having.</summary>
public class NotificationPreferenceDto
{
    public bool ChatEnabled { get; set; } = true;

    public bool OrdersEnabled { get; set; } = true;

    public bool VerificationEnabled { get; set; } = true;

    public bool MarketplaceEnabled { get; set; } = true;

    public bool AnnouncementsEnabled { get; set; } = true;

    public bool EmailEnabled { get; set; }

    public bool PushEnabled { get; set; }
}
