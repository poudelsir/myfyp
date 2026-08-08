using System.ComponentModel.DataAnnotations;
using SajhaSikshya.Constants;
using SajhaSikshya.Data.Enums;

namespace SajhaSikshya.Data.Entities.Notifications;

/// <summary>
/// A single in-app notification for one user. The single persisted record every
/// module in the system creates through <see cref="Services.Interfaces.Notifications.INotificationService"/>
/// instead of writing its own ad-hoc notification logic — Orders, Verification, Chat,
/// and future modules (Reviews, AI, Admin) all funnel through the same table and the
/// same real-time delivery path. <see cref="BaseEntity.CreatedAtUtc"/>/<see cref="BaseEntity.IsDeleted"/>
/// already satisfy the spec's "Notification.CreatedAtUtc" field and "soft delete
/// support" — nothing extra needed for either.
/// </summary>
public class Notification : BaseEntity
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public NotificationType NotificationType { get; set; }

    [Required]
    [StringLength(NotificationConstants.MaximumTitleLength)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(NotificationConstants.MaximumMessageLength)]
    public string Message { get; set; } = string.Empty;

    /// <summary>Relative in-app URL the notification navigates to when clicked (e.g. "/Student/Chat/Conversation/12"). Null for notifications with nothing to navigate to.</summary>
    [StringLength(NotificationConstants.MaximumLinkLength)]
    public string? Link { get; set; }

    public bool IsRead { get; set; }

    public DateTime? ReadAtUtc { get; set; }

    /// <summary>
    /// Who/what triggered this notification — another user's id (e.g. a chat message's
    /// sender), an admin's id (e.g. who approved a listing), or null for a purely
    /// system-generated notice. Deliberately a plain string, not a second FK/navigation
    /// to Users, the same "ceremony without payoff" reasoning as <c>Order.CreatedByUserId</c> —
    /// nothing renders "created by" as a linked profile, only as plain text if at all.
    /// </summary>
    [StringLength(450)]
    public string? CreatedBy { get; set; }
}
