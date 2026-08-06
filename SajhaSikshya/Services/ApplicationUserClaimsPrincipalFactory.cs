using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SajhaSikshya.Data.Entities;

namespace SajhaSikshya.Services;

/// <summary>
/// Extends Identity's default claims principal with the user's given/family name so
/// the UI (navbar, sidebar) can display "Jane Doe" instead of falling back to the
/// login email. Runs once per sign-in/cookie refresh; keeping it here rather than
/// querying the database from a view is what lets <c>Views/Shared/_Navbar.cshtml</c>
/// read the name straight off <c>User</c> with no extra data access.
/// </summary>
public class ApplicationUserClaimsPrincipalFactory
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
{
    public ApplicationUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> optionsAccessor)
        : base(userManager, roleManager, optionsAccessor)
    {
    }

    public override async Task<ClaimsPrincipal> CreateAsync(ApplicationUser user)
    {
        var principal = await base.CreateAsync(user);

        if (principal.Identity is ClaimsIdentity identity)
        {
            identity.AddClaim(new Claim(ClaimTypes.GivenName, user.FirstName));
            identity.AddClaim(new Claim(ClaimTypes.Surname, user.LastName));
        }

        return principal;
    }
}
