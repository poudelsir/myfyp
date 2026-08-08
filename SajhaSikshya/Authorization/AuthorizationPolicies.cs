namespace SajhaSikshya.Authorization;

/// <summary>
/// Centralized, strongly-typed authorization policy names — same rationale as
/// <see cref="Data.Constants.Roles"/> for role names: referencing this constant instead
/// of a magic string in <c>[Authorize(Policy = "...")]</c> prevents typos and keeps
/// every policy name discoverable from one place.
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>
    /// Requires the current user to have an approved Student Verification (see
    /// <see cref="VerifiedStudentRequirement"/>/<see cref="VerifiedStudentHandler"/>).
    /// Registered and ready to use, but not yet applied to Create Listing, Purchase,
    /// Donate, or Chat — Phase 5 only prepares this infrastructure, per the project's
    /// explicit instruction not to enforce it yet. A future phase applies it with
    /// <c>[Authorize(Policy = AuthorizationPolicies.VerifiedStudent)]</c>.
    /// </summary>
    public const string VerifiedStudent = "VerifiedStudent";
}
