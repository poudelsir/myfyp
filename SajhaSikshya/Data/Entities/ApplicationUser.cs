using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace SajhaSikshya.Data.Entities;

/// <summary>
/// Application-specific Identity user. Extends the default <see cref="IdentityUser"/>
/// with the profile fields SajhaSikshya needs (name, avatar, auditing) while keeping
/// all authentication concerns (password hash, security stamp, lockout, etc.) in the
/// base class managed by ASP.NET Core Identity.
/// </summary>
public class ApplicationUser : IdentityUser
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    [PersonalData]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 2)]
    [PersonalData]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Relative path (under wwwroot) to the user's profile picture.
    /// Null falls back to a generated initials avatar in the UI.
    /// </summary>
    [StringLength(300)]
    public string? ProfilePicturePath { get; set; }

    [StringLength(300)]
    public string? Address { get; set; }

    /// <summary>Free-text, self-reported school/college/university/company — a profile detail anyone can set, distinct from <see cref="Entities.Verification.StudentVerification.InstitutionName"/> which is the admin-approved seller credential and is never duplicated here.</summary>
    [StringLength(150)]
    public string? Institution { get; set; }

    [StringLength(500)]
    public string? Bio { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAtUtc { get; set; }

    /// <summary>
    /// Allows administrators to disable an account without deleting it,
    /// preserving referential integrity with the user's historical data.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Settings &gt; Privacy toggle. When false, <c>MarketplaceController.Seller</c> hides
    /// this user's public seller profile from everyone except themselves and Admins —
    /// their listings/marketplace presence are unaffected, only the profile page itself.
    /// Defaults true (matches today's always-visible behavior) so existing users see no
    /// change until they opt out.
    /// </summary>
    public bool IsPublicProfile { get; set; } = true;

    [NotMapped]
    public string FullName => $"{FirstName} {LastName}".Trim();
}
