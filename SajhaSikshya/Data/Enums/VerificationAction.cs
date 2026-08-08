using System.ComponentModel.DataAnnotations;

namespace SajhaSikshya.Data.Enums;

/// <summary>
/// The action an admin took the last time a <see cref="Entities.Verification.StudentVerification"/>
/// request was reviewed. Distinct from <see cref="VerificationStatus"/> the same way
/// <see cref="ModerationAction"/> is distinct from <see cref="ListingStatus"/> — this
/// records the verb, while VerificationStatus records the resulting state. Needed
/// because Reject and RequestResubmission both leave a request at
/// <see cref="VerificationStatus.Rejected"/>, but are different admin actions worth
/// telling apart in the audit trail (a flat rejection vs. "please upload a clearer
/// document and try again").
/// </summary>
public enum VerificationAction
{
    [Display(Name = "Approved", Description = "Verified as a genuine student.")]
    Approve = 1,

    [Display(Name = "Rejected", Description = "Rejected — does not qualify for verification.")]
    Reject = 2,

    [Display(Name = "Resubmission Requested", Description = "Documentation was unclear or incomplete; asked to resubmit.")]
    RequestResubmission = 3,
}
