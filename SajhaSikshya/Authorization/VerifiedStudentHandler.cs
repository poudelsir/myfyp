using Microsoft.AspNetCore.Authorization;
using SajhaSikshya.Extensions;
using SajhaSikshya.Services.Interfaces.Verification;

namespace SajhaSikshya.Authorization;

/// <summary>
/// Satisfies <see cref="VerifiedStudentRequirement"/> by asking
/// <see cref="IVerificationQueryService.IsUserVerifiedAsync"/> — a live database check,
/// deliberately not a cached/claims-based one. This is the one place real authorization
/// decisions get made (once this policy is actually applied to something in a future
/// phase), so it has to reflect an admin's Approve/Reject decision immediately; the
/// "Verified Student" badge shown around the UI can afford to be a plain per-request
/// query too since there's only ever one such call per page, but a security decision
/// specifically must never trust anything that could be stale (e.g. a login-time claim).
/// An anonymous user (no user id on the principal) simply never succeeds — there is no
/// separate failure branch to write, since not calling <see cref="AuthorizationHandlerContext.Succeed"/>
/// is itself a failure by default.
/// </summary>
public class VerifiedStudentHandler : AuthorizationHandler<VerifiedStudentRequirement>
{
    private readonly IVerificationQueryService _verificationQueryService;

    public VerifiedStudentHandler(IVerificationQueryService verificationQueryService)
    {
        _verificationQueryService = verificationQueryService;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, VerifiedStudentRequirement requirement)
    {
        var userId = context.User.GetUserId();
        if (userId is null)
        {
            return;
        }

        if (await _verificationQueryService.IsUserVerifiedAsync(userId))
        {
            context.Succeed(requirement);
        }
    }
}
