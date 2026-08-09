using SajhaSikshya.ViewModels.Account;
using SajhaSikshya.ViewModels.Student.Profile;

namespace SajhaSikshya.ViewModels.Admin.Profile;

/// <summary>
/// "My Profile" for an Administrator — same Personal Information/Security building
/// blocks as the Student profile page (<see cref="PersonalInfoViewModel"/>,
/// <see cref="ChangePasswordViewModel"/> are reused as-is, not duplicated), but the
/// account-info section is Admin-shaped (role/status/last login) instead of
/// seller/verification-shaped, since an Administrator can't be a seller.
/// </summary>
public class AdminProfileIndexViewModel
{
    public PersonalInfoViewModel PersonalInfo { get; set; } = new();

    /// <summary>Always a fresh, empty form unless a failed submission is being redisplayed — never pre-filled with the user's actual (hashed) password data.</summary>
    public ChangePasswordViewModel ChangePassword { get; set; } = new();

    public bool EmailConfirmed { get; set; }

    public bool PhoneNumberConfirmed { get; set; }

    public bool IsActive { get; set; }

    public DateTime MemberSinceUtc { get; set; }

    public DateTime? LastLoginAtUtc { get; set; }
}
