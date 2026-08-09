using SajhaSikshya.Data.Enums;

namespace SajhaSikshya.DTOs.Users;

/// <summary>One row of the Admin "Manage Users" list. Verification/seller-status fields are read live from the verification module, never duplicated onto the user record itself.</summary>
public class AdminUserListItemDto
{
    public string Id { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string? ProfilePicturePath { get; set; }

    public string Role { get; set; } = string.Empty;

    public bool IsVerifiedSeller { get; set; }

    public VerificationStatus? VerificationStatus { get; set; }

    public string? VerificationStatusDisplay { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
