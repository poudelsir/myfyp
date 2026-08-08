using SajhaSikshya.Data.Enums;

namespace SajhaSikshya.Services.Verification;

/// <summary>
/// The single place that knows which <see cref="VerificationStatus"/> transitions are
/// legal and what <see cref="VerificationAction"/> a review produces — so
/// <see cref="VerificationService.ReviewAsync"/> doesn't hand-roll this logic inline
/// and no second code path can quietly diverge from it. Mirrors why
/// <c>ModerationAction</c> exists separately from <c>ListingStatus</c> in the
/// Marketplace module: Reject and RequestResubmission both leave a request at
/// <see cref="VerificationStatus.Rejected"/>, but are different actions worth
/// recording distinctly.
/// </summary>
public static class VerificationState
{
    /// <summary>Only a request still awaiting review can be reviewed — an already-decided request (Verified or Rejected) requires a fresh resubmission, not a second review of the same row.</summary>
    public static bool CanReview(VerificationStatus currentStatus) => currentStatus == VerificationStatus.Pending;

    /// <summary>The <see cref="VerificationStatus"/> a given review action produces.</summary>
    public static VerificationStatus Apply(VerificationAction action) => action switch
    {
        VerificationAction.Approve => VerificationStatus.Verified,
        VerificationAction.Reject => VerificationStatus.Rejected,
        VerificationAction.RequestResubmission => VerificationStatus.Rejected,
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Unrecognized verification action."),
    };

    /// <summary>Reject and RequestResubmission both require a reason (it's what the student sees and acts on); Approve does not.</summary>
    public static bool RequiresReason(VerificationAction action) =>
        action is VerificationAction.Reject or VerificationAction.RequestResubmission;
}
