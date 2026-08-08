using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SajhaSikshya.Constants;
using SajhaSikshya.Data.Entities;
using SajhaSikshya.Data.Entities.Notifications;
using SajhaSikshya.Data.Enums;
using SajhaSikshya.DTOs.Notifications;
using SajhaSikshya.Mappings.Notifications;
using SajhaSikshya.Repositories.Interfaces;
using SajhaSikshya.Services.Interfaces.Notifications;

namespace SajhaSikshya.Services.Notifications;

/// <summary>
/// Implements <see cref="INotificationService"/>. Every mutation notifies
/// <see cref="_dispatcher"/> after its own <see cref="IUnitOfWork.SaveChangesAsync"/>
/// commits, the same "never broadcast a change that then fails to persist" discipline
/// <c>ChatService</c> established. This is the only class in the system that ever
/// writes to the <c>Notifications</c> table. <see cref="CreateAsync"/> and
/// <see cref="CreateBroadcastAsync"/> share one private creation path
/// (<see cref="CreateIfEnabledAsync"/>) so a single notification and a fanned-out
/// broadcast can never drift apart on preference-checking, truncation, or dispatch
/// behavior.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _dispatcher;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IUnitOfWork unitOfWork,
        INotificationDispatcher dispatcher,
        UserManager<ApplicationUser> userManager,
        ILogger<NotificationService> logger)
    {
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<ServiceResult<int>> CreateAsync(
        string userId,
        NotificationType notificationType,
        string title,
        string message,
        string? link = null,
        string? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message))
        {
            return ServiceResult<int>.Failure("Notification title and message are required.");
        }

        var id = await CreateIfEnabledAsync(userId, notificationType, title, message, link, createdBy);
        return ServiceResult<int>.Success(id ?? 0);
    }

    public async Task<ServiceResult<int>> CreateBroadcastAsync(
        NotificationType notificationType,
        string title,
        string message,
        string? link,
        string createdByUserId,
        string? targetRole = null)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message))
        {
            return ServiceResult<int>.Failure("Notification title and message are required.");
        }

        var recipientIds = string.IsNullOrWhiteSpace(targetRole)
            ? await _userManager.Users.AsNoTracking().Where(u => u.IsActive).Select(u => u.Id).ToListAsync()
            : (await _userManager.GetUsersInRoleAsync(targetRole)).Where(u => u.IsActive).Select(u => u.Id).ToList();

        var sentCount = 0;
        foreach (var recipientId in recipientIds)
        {
            var id = await CreateIfEnabledAsync(recipientId, notificationType, title, message, link, createdByUserId);
            if (id is not null)
            {
                sentCount++;
            }
        }

        _logger.LogInformation(
            "Broadcast notification ({Type}) sent to {SentCount}/{TotalCount} users (role={Role}) by {AdminId}",
            notificationType, sentCount, recipientIds.Count, targetRole ?? "Everyone", createdByUserId);

        return ServiceResult<int>.Success(sentCount);
    }

    public async Task<ServiceResult> MarkAsReadAsync(int notificationId, string userId)
    {
        var repository = _unitOfWork.Repository<Notification>();
        var notification = await repository.GetByIdAsync(notificationId);
        if (notification is null || notification.UserId != userId)
        {
            return ServiceResult.Failure("Notification not found.");
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAtUtc = DateTime.UtcNow;
            repository.Update(notification);
            await _unitOfWork.SaveChangesAsync();

            await _dispatcher.NotificationReadAsync(userId, notificationId);
            await _dispatcher.UnreadCountChangedAsync(userId, await GetUnreadCountForUserAsync(userId));
        }

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> MarkAllAsReadAsync(string userId)
    {
        var repository = _unitOfWork.Repository<Notification>();
        var unread = await repository.FindAsync(n => n.UserId == userId && !n.IsRead);
        if (unread.Count == 0)
        {
            return ServiceResult.Success();
        }

        var now = DateTime.UtcNow;
        foreach (var notification in unread)
        {
            notification.IsRead = true;
            notification.ReadAtUtc = now;
            repository.Update(notification);
        }

        await _unitOfWork.SaveChangesAsync();

        await _dispatcher.AllNotificationsReadAsync(userId);
        await _dispatcher.UnreadCountChangedAsync(userId, 0);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeleteAsync(int notificationId, string userId)
    {
        var repository = _unitOfWork.Repository<Notification>();
        var notification = await repository.GetByIdAsync(notificationId);
        if (notification is null || notification.UserId != userId)
        {
            return ServiceResult.Failure("Notification not found.");
        }

        var wasUnread = !notification.IsRead;
        repository.Remove(notification);
        await _unitOfWork.SaveChangesAsync();

        await _dispatcher.NotificationDeletedAsync(userId, notificationId);
        if (wasUnread)
        {
            await _dispatcher.UnreadCountChangedAsync(userId, await GetUnreadCountForUserAsync(userId));
        }

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> UpdatePreferencesAsync(string userId, NotificationPreferenceDto preferences)
    {
        var repository = _unitOfWork.Repository<NotificationPreference>();
        var existing = await repository.FirstOrDefaultAsync(p => p.UserId == userId);
        var isNew = existing is null;
        existing ??= new NotificationPreference { UserId = userId };

        existing.ChatEnabled = preferences.ChatEnabled;
        existing.OrdersEnabled = preferences.OrdersEnabled;
        existing.VerificationEnabled = preferences.VerificationEnabled;
        existing.MarketplaceEnabled = preferences.MarketplaceEnabled;
        existing.AnnouncementsEnabled = preferences.AnnouncementsEnabled;
        // EmailEnabled/PushEnabled are saved like any other toggle even though nothing
        // reads them yet — this is exactly the "prepare the extension point" the spec
        // asked for: the schema and the save path both already work; only the actual
        // email/push delivery step is missing, deliberately.
        existing.EmailEnabled = preferences.EmailEnabled;
        existing.PushEnabled = preferences.PushEnabled;

        if (isNew)
        {
            await repository.AddAsync(existing);
        }
        else
        {
            repository.Update(existing);
        }

        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Success();
    }

    /// <summary>
    /// The one place a notification actually gets built, persisted, and dispatched —
    /// used by both <see cref="CreateAsync"/> (one recipient) and
    /// <see cref="CreateBroadcastAsync"/> (many). Returns null without creating
    /// anything if <paramref name="userId"/> has disabled this
    /// <paramref name="notificationType"/>'s category — a suppression, not an error.
    /// </summary>
    private async Task<int?> CreateIfEnabledAsync(
        string userId,
        NotificationType notificationType,
        string title,
        string message,
        string? link,
        string? createdBy)
    {
        if (!await IsCategoryEnabledAsync(userId, notificationType))
        {
            _logger.LogInformation("Notification suppressed for user {UserId} — {Type} notifications disabled by preference", userId, notificationType);
            return null;
        }

        var notification = new Notification
        {
            UserId = userId,
            NotificationType = notificationType,
            // Defensively truncated here rather than trusting every caller to pre-size
            // its own text — this is the one central write path, so bounding it once
            // here protects every integration (Orders/Verification/Chat/Marketplace)
            // from a DbUpdateException if a template ever produces oversized text (e.g.
            // a chat message's 2000-character cap exceeds a notification's 500-character one).
            Title = Truncate(title.Trim(), NotificationConstants.MaximumTitleLength),
            Message = Truncate(message.Trim(), NotificationConstants.MaximumMessageLength),
            Link = string.IsNullOrWhiteSpace(link) ? null : Truncate(link.Trim(), NotificationConstants.MaximumLinkLength),
            CreatedBy = createdBy,
        };

        var repository = _unitOfWork.Repository<Notification>();
        await repository.AddAsync(notification);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Notification {NotificationId} ({Type}) created for user {UserId}", notification.Id, notificationType, userId);

        await _dispatcher.NotificationCreatedAsync(userId, notification.ToDto());
        await _dispatcher.UnreadCountChangedAsync(userId, await GetUnreadCountForUserAsync(userId));

        return notification.Id;
    }

    /// <summary>
    /// <see cref="NotificationType.System"/>, <see cref="NotificationType.Review"/>, and
    /// <see cref="NotificationType.AIRecommendation"/> are deliberately NOT user-
    /// toggleable — none of the five preference categories the spec asked for
    /// (Chat/Orders/Verification/Marketplace/Announcements) covers them, and nothing in
    /// this codebase raises a Review or AIRecommendation notification yet, so there's
    /// no real preference to honor for those; System notices are treated as always-on
    /// (a maintenance/policy notice shouldn't be silenceable the way a chat ping is).
    /// A missing preference row (the common case — most users never visit Preferences)
    /// means every category defaults to enabled, matching <see cref="NotificationPreferenceDto"/>'s
    /// own field defaults.
    /// </summary>
    private async Task<bool> IsCategoryEnabledAsync(string userId, NotificationType notificationType)
    {
        if (notificationType is NotificationType.System or NotificationType.Review or NotificationType.AIRecommendation)
        {
            return true;
        }

        var preference = await _unitOfWork.Repository<NotificationPreference>().FirstOrDefaultAsync(p => p.UserId == userId);
        if (preference is null)
        {
            return true;
        }

        return notificationType switch
        {
            NotificationType.Message => preference.ChatEnabled,
            NotificationType.Order => preference.OrdersEnabled,
            NotificationType.Verification => preference.VerificationEnabled,
            NotificationType.Listing => preference.MarketplaceEnabled,
            NotificationType.Announcement => preference.AnnouncementsEnabled,
            _ => true,
        };
    }

    /// <summary>Deliberately duplicates the one-line query <c>NotificationQueryService.GetUnreadCountAsync</c> also runs — same "a duplicated one-liner is cheaper than a command-depending-on-query dependency" reasoning as <c>ChatService</c>'s identical helper.</summary>
    private async Task<int> GetUnreadCountForUserAsync(string userId) =>
        await _unitOfWork.Repository<Notification>().CountAsync(n => n.UserId == userId && !n.IsRead);

    private static string Truncate(string value, int maxLength) =>
        value.Length > maxLength ? value[..maxLength] : value;
}
