using SajhaSikshya.DTOs.Notifications;
using SajhaSikshya.DTOs.Verification;
using SajhaSikshya.ViewModels.Account;
using SajhaSikshya.ViewModels.Student.Profile;

namespace SajhaSikshya.ViewModels.Student.Settings;

/// <summary>
/// Composes the Student Settings page's five tabs from existing modules — never a new
/// source of truth. <see cref="PersonalInfo"/>/<see cref="ChangePassword"/> post to the
/// same <c>ProfileController</c> actions the Profile page uses;
/// <see cref="NotificationPreferences"/> posts to the same root-level
/// <c>NotificationsController.Preferences</c> every area already shares;
/// <see cref="Verification"/> feeds the same <c>_SellerStatusPanel</c> partial the
/// Profile page renders. Only <see cref="IsPublicProfile"/> is genuinely new state.
/// </summary>
public class StudentSettingsViewModel
{
    public PersonalInfoViewModel PersonalInfo { get; set; } = new();

    public ChangePasswordViewModel ChangePassword { get; set; } = new();

    public NotificationPreferenceDto NotificationPreferences { get; set; } = new();

    public bool IsPublicProfile { get; set; } = true;

    public VerificationDto? Verification { get; set; }
}
