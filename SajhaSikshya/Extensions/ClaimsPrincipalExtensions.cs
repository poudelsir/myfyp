using System.Security.Claims;

namespace SajhaSikshya.Extensions;

/// <summary>
/// Convenience accessors for the common claims Identity puts on <see cref="ClaimsPrincipal"/>,
/// so controllers/views read `User.GetUserId()` instead of repeating
/// `User.FindFirstValue(ClaimTypes.NameIdentifier)` everywhere.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    public static string? GetUserId(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.NameIdentifier);

    public static string? GetEmail(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.Email);

    public static string? GetFullName(this ClaimsPrincipal principal)
    {
        var firstName = principal.FindFirstValue(ClaimTypes.GivenName);
        var lastName = principal.FindFirstValue(ClaimTypes.Surname);

        if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName))
        {
            return null;
        }

        return $"{firstName} {lastName}".Trim();
    }
}
