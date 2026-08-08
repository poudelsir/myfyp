using System.ComponentModel.DataAnnotations;
using SajhaSikshya.Constants;
using SajhaSikshya.Data.Enums;

namespace SajhaSikshya.ViewModels.Admin.Notifications;

/// <summary>Backs the Admin "Broadcast Notification" form — System Announcements, Maintenance Notices, and role-targeted notices all go through this one form, distinguished only by which <see cref="NotificationType"/> and <see cref="TargetRole"/> the admin picks.</summary>
public class BroadcastNotificationViewModel
{
    [Required]
    [StringLength(NotificationConstants.MaximumTitleLength)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(NotificationConstants.MaximumMessageLength)]
    public string Message { get; set; } = string.Empty;

    [StringLength(NotificationConstants.MaximumLinkLength)]
    public string? Link { get; set; }

    public NotificationType NotificationType { get; set; } = NotificationType.Announcement;

    /// <summary>Null/empty means every active user; otherwise a role name (<see cref="Data.Constants.Roles.Student"/>/<see cref="Data.Constants.Roles.Admin"/>) scopes the broadcast to just that role.</summary>
    public string? TargetRole { get; set; }
}
