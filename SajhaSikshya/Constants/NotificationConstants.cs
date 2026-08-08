namespace SajhaSikshya.Constants;

/// <summary>Business-rule limits for the Notification module (Phase 8), mirroring how <see cref="ChatConstants"/> centralizes the Chat module's limits.</summary>
public static class NotificationConstants
{
    public const int MaximumTitleLength = 150;

    public const int MaximumMessageLength = 500;

    public const int MaximumLinkLength = 300;

    /// <summary>How many notifications the navbar bell's dropdown shows before "View All" — the full history is <see cref="Controllers.NotificationsController.Index"/>, paginated with <see cref="PaginationConstants.DefaultPageSize"/>.</summary>
    public const int RecentNotificationsCount = 5;
}
