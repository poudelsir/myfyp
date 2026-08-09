using SajhaSikshya.DTOs.Notifications;
using SajhaSikshya.DTOs.Reviews;
using SajhaSikshya.DTOs.Verification;

namespace SajhaSikshya.DTOs.Users;

/// <summary>
/// The Admin User Details page's full composition — personal info owned by
/// <see cref="Data.Entities.ApplicationUser"/>, plus read-only summaries pulled live
/// from the Verification/Listing/Order/Review/Notification modules (never duplicated,
/// never a new source of truth). "Recent Activity" is intentionally just
/// <see cref="VerificationHistory"/> + <see cref="RecentNotifications"/> rather than a
/// new cross-module activity aggregator.
/// </summary>
public class AdminUserDetailDto
{
    public string Id { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string? Address { get; set; }

    public string? Institution { get; set; }

    public string? Bio { get; set; }

    public string? ProfilePicturePath { get; set; }

    public string Role { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? LastLoginAtUtc { get; set; }

    public bool EmailConfirmed { get; set; }

    public bool PhoneNumberConfirmed { get; set; }

    /// <summary>The user's current (latest) verification row, if they've ever applied.</summary>
    public VerificationDto? CurrentVerification { get; set; }

    public bool IsVerifiedSeller { get; set; }

    public int ListingCount { get; set; }

    public int BuyerOrderCount { get; set; }

    public int SellerOrderCount { get; set; }

    public ReputationDto? Reputation { get; set; }

    public int UnreadNotificationCount { get; set; }

    public IReadOnlyList<NotificationDto> RecentNotifications { get; set; } = Array.Empty<NotificationDto>();

    public PagedResult<VerificationDto> VerificationHistory { get; set; } = new();
}
