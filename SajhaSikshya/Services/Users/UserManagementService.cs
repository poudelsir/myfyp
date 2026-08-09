using Microsoft.AspNetCore.Identity;
using SajhaSikshya.Data.Entities;
using SajhaSikshya.Data.Enums;
using SajhaSikshya.Services.Interfaces.Notifications;
using SajhaSikshya.Services.Interfaces.Users;

namespace SajhaSikshya.Services.Users;

public class UserManagementService : IUserManagementService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly INotificationService _notificationService;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(
        UserManager<ApplicationUser> userManager,
        INotificationService notificationService,
        ILogger<UserManagementService> logger)
    {
        _userManager = userManager;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<ServiceResult> SetActiveStatusAsync(string userId, bool isActive, string adminId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return ServiceResult.Failure("User not found.");
        }

        if (user.IsActive == isActive)
        {
            return ServiceResult.Success();
        }

        user.IsActive = isActive;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return ServiceResult.Failure(result.Errors.Select(e => e.Description).ToArray());
        }

        if (!isActive)
        {
            // Forces any already-signed-in session to re-authenticate within
            // SecurityConstants.SecurityStampValidationIntervalMinutes rather than
            // continuing to work off a stale cookie until it naturally expires.
            await _userManager.UpdateSecurityStampAsync(user);
        }

        var (title, message) = isActive
            ? ("Account reactivated", "Your account has been reactivated. You can sign in again.")
            : ("Account suspended", "Your account has been suspended by an administrator.");

        await _notificationService.CreateAsync(userId, NotificationType.System, title, message, createdBy: adminId);

        _logger.LogInformation("User {UserId} {Action} by admin {AdminId}", userId, isActive ? "reactivated" : "suspended", adminId);

        return ServiceResult.Success();
    }
}
