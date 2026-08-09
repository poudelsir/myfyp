using SajhaSikshya.Data.Enums;
using SajhaSikshya.ViewModels.Verification;

namespace SajhaSikshya.Services.Interfaces.Verification;

/// <summary>
/// Student Verification mutations (Phase 5). Split from
/// <see cref="IVerificationQueryService"/> the same way the Marketplace module splits
/// <c>IListingService</c> from <c>IListingQueryService"/></c> — commands here, reads
/// there. <see cref="CreateAsync"/>/<see cref="ResubmitAsync"/> are the student-facing
/// half; <see cref="ReviewAsync"/> is the single admin-review entry point, mirroring
/// <c>IListingService.ModerateListingAsync</c> — one method dispatching on
/// <see cref="VerificationAction"/> rather than three separate Approve/Reject/
/// RequestResubmission methods, so the "must be Pending" precondition and the
/// Reviewer/Timestamp/Reason audit stamp are asserted in exactly one place
/// (see <see cref="Verification.VerificationState"/>) instead of three.
/// </summary>
public interface IVerificationService
{
    /// <summary>
    /// A user's first-ever verification submission. Fails if they already have a
    /// Pending request, or if the chosen university doesn't exist/isn't active.
    /// Uploads and validates the student ID image via
    /// <see cref="IImageStorageService"/> before creating the record; the saved file is
    /// rolled back if the database write then fails, so a failed submission never
    /// leaves an orphaned file behind.
    /// </summary>
    Task<ServiceResult<int>> CreateAsync(string userId, VerificationSubmissionViewModel model);

    /// <summary>
    /// Submits a new verification attempt after a prior one was rejected. Creates a
    /// brand new <c>StudentVerification</c> row rather than editing the rejected one —
    /// see <see cref="Verification.VerificationService"/>'s remarks — so this shares
    /// every validation rule with <see cref="CreateAsync"/> (still fails if a Pending
    /// request already exists, which a resubmission naturally creates going forward).
    /// </summary>
    Task<ServiceResult<int>> ResubmitAsync(string userId, VerificationSubmissionViewModel model);

    /// <summary>
    /// The single admin review entry point — Approve, Reject, or RequestResubmission,
    /// selected by <paramref name="action"/>. Fails unless the request is currently
    /// Pending (see <see cref="Verification.VerificationState.CanReview"/>), and unless
    /// <paramref name="reason"/> is supplied for the two actions that require one (Reject,
    /// RequestResubmission — Approve does not). On success, stamps Reviewer/Timestamp/
    /// Reason/Action onto the record in one place, satisfying Phase 5's audit
    /// requirement regardless of which action was taken.
    /// </summary>
    Task<ServiceResult> ReviewAsync(int verificationId, VerificationAction action, string reviewerId, string? reason = null, string? adminNotes = null);

    /// <summary>
    /// Updates only the Selling Categories on a seller's current (latest) verification
    /// row — the one field the Seller Profile page allows editing in place, since it's
    /// declared as moderation/insights-only metadata rather than an identity claim.
    /// Fails unless the caller's current row is <see cref="Data.Enums.VerificationStatus.Verified"/>;
    /// every other field (Seller Type, Institution, documents) requires a full
    /// re-verification via <see cref="ResubmitAsync"/> instead.
    /// </summary>
    Task<ServiceResult> UpdateSellingCategoriesAsync(string userId, List<SellingCategory> categories);
}
