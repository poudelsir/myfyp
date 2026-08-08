using System.ComponentModel.DataAnnotations;

namespace SajhaSikshya.Data.Enums;

/// <summary>
/// Review state of a user's student verification submission (e.g. student ID
/// upload). Selling on the marketplace will require <see cref="Verified"/> status.
/// </summary>
public enum VerificationStatus
{
    /// <summary>Submitted and awaiting admin review.</summary>
    [Display(Name = "Pending", Description = "Submitted and awaiting admin review.")]
    Pending = 0,

    /// <summary>Reviewed and confirmed as a valid student.</summary>
    [Display(Name = "Verified", Description = "Reviewed and confirmed as a valid student.")]
    Verified = 1,

    /// <summary>Reviewed and rejected; the submitted documentation did not meet requirements.</summary>
    [Display(Name = "Rejected", Description = "Reviewed and rejected; documentation did not meet requirements.")]
    Rejected = 2,
}
