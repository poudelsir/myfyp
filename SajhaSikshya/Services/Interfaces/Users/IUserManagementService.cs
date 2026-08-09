namespace SajhaSikshya.Services.Interfaces.Users;

/// <summary>Admin mutations against a user account. Approve/Reject Seller are deliberately not here — they go through the existing, unmodified <c>IVerificationService.ReviewAsync</c> via the Verification admin controller, reused rather than duplicated.</summary>
public interface IUserManagementService
{
    /// <summary>
    /// Suspends or reactivates a user. Suspending also bumps the user's Identity
    /// security stamp so an already-signed-in session is forced to re-authenticate
    /// (see <see cref="Constants.SecurityConstants.SecurityStampValidationIntervalMinutes"/>),
    /// and notifies the affected user either way.
    /// </summary>
    Task<ServiceResult> SetActiveStatusAsync(string userId, bool isActive, string adminId);
}
