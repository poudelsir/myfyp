using SajhaSikshya.DTOs.Verification;
using SajhaSikshya.ViewModels.Account;

namespace SajhaSikshya.ViewModels.Student.Profile;

/// <summary>Everything the "My Profile" page needs — assembled by the controller from <see cref="Data.Entities.ApplicationUser"/> and the verification module; never a new source of truth, just a read/display composition.</summary>
public class ProfileIndexViewModel
{
    public PersonalInfoViewModel PersonalInfo { get; set; } = new();

    /// <summary>Always a fresh, empty form unless a failed submission is being redisplayed — never pre-filled with the user's actual (hashed) password data.</summary>
    public ChangePasswordViewModel ChangePassword { get; set; } = new();

    /// <summary>The seller's current (latest) verification row, or null if never submitted — see <see cref="Services.Interfaces.Verification.IVerificationQueryService.GetCurrentStatusAsync"/>.</summary>
    public VerificationDto? Verification { get; set; }

    public bool EmailConfirmed { get; set; }

    public bool PhoneNumberConfirmed { get; set; }

    public DateTime MemberSinceUtc { get; set; }
}
