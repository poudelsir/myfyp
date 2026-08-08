namespace SajhaSikshya.Data.Entities.Notifications;

/// <summary>
/// One row per user, created lazily the first time they actually change a setting
/// (see <see cref="Services.Notifications.NotificationService.UpdatePreferencesAsync"/>) —
/// a user who never visits Preferences simply has no row, and every category is
/// treated as enabled by default (see <see cref="Services.Notifications.NotificationService"/>'s
/// preference-check helper). A fixed set of boolean columns rather than a normalized
/// "one row per (user, category)" table — there are exactly five toggleable categories
/// and they never change dynamically, so a wide row is simpler to read, write, and
/// query (one round trip either way) with no real flexibility given up.
/// </summary>
public class NotificationPreference : BaseEntity
{
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    /// <summary>Gates <see cref="Data.Enums.NotificationType.Message"/> (Chat).</summary>
    public bool ChatEnabled { get; set; } = true;

    /// <summary>Gates <see cref="Data.Enums.NotificationType.Order"/>.</summary>
    public bool OrdersEnabled { get; set; } = true;

    /// <summary>Gates <see cref="Data.Enums.NotificationType.Verification"/>.</summary>
    public bool VerificationEnabled { get; set; } = true;

    /// <summary>Gates <see cref="Data.Enums.NotificationType.Listing"/>.</summary>
    public bool MarketplaceEnabled { get; set; } = true;

    /// <summary>Gates <see cref="Data.Enums.NotificationType.Announcement"/>.</summary>
    public bool AnnouncementsEnabled { get; set; } = true;

    /// <summary>
    /// Schema preparation for a future email-delivery channel — not read or written by
    /// any logic in this phase beyond the toggle itself. Defaults to false (opt-in,
    /// not opt-out) since no email is ever actually sent yet; flipping this today has
    /// no observable effect.
    /// </summary>
    public bool EmailEnabled { get; set; }

    /// <summary>Same forward-preparation as <see cref="EmailEnabled"/>, for a future push-notification channel.</summary>
    public bool PushEnabled { get; set; }
}
