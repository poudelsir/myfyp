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

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAtUtc { get; set; }

    /// <summary>
    /// Allows administrators to disable an account without deleting it,
    /// preserving referential integrity with the user's historical data.
    /// </summary>
    public bool IsActive { get; set; } = true;

    [NotMapped]
    public string FullName => $"{FirstName} {LastName}".Trim();
}
