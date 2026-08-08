using Microsoft.AspNetCore.Authorization;

namespace SajhaSikshya.Authorization;

/// <summary>
/// Marker requirement for the <c>VerifiedStudent</c> policy (Phase 5) — satisfied when
/// the current user has an approved <see cref="Data.Enums.VerificationStatus.Verified"/>
/// Student Verification. Carries no data of its own; <see cref="VerifiedStudentHandler"/>
/// does the actual check. Not applied to anything yet (Create Listing, Purchase, Donate,
/// Chat) — this phase only wires up the infrastructure so a future phase can add
/// <c>[Authorize(Policy = AuthorizationPolicies.VerifiedStudent)]</c> without having to
/// build any of this first.
/// </summary>
public class VerifiedStudentRequirement : IAuthorizationRequirement
{
}
