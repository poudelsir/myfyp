using SajhaSikshya.DTOs.Notifications;
using SajhaSikshya.ViewModels.Account;
using SajhaSikshya.ViewModels.Student.Profile;

namespace SajhaSikshya.ViewModels.Admin.Settings;

/// <summary>
/// Composes the Admin Settings page's tabs from existing modules — same composition
/// pattern as <see cref="Student.Settings.StudentSettingsViewModel"/>, minus Privacy
/// and Seller (neither applies to an Administrator account) and with a System
/// Preferences tab instead, which surfaces existing admin-configurable areas
/// (Categories, Universities) rather than inventing new backend configuration.
/// </summary>
public class AdminSettingsViewModel
{
    public PersonalInfoViewModel PersonalInfo { get; set; } = new();

    public ChangePasswordViewModel ChangePassword { get; set; } = new();

    public NotificationPreferenceDto NotificationPreferences { get; set; } = new();

    public bool IsActive { get; set; }
}
