using SajhaSikshya.Data.Entities;
using SajhaSikshya.DTOs;

namespace SajhaSikshya.Mappings;

/// <summary>
/// Explicit entity-to-DTO mapping for <see cref="ApplicationUser"/>. The project uses
/// plain extension methods rather than a mapping library (AutoMapper, Mapster) since
/// the mapping surface is small and hand-written mappings are easier to debug and
/// keep free of reflection overhead; revisit if the number of mapped types grows.
/// </summary>
public static class UserMappings
{
    public static UserDto ToDto(this ApplicationUser user, IList<string> roles)
    {
        return new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            ProfilePicturePath = user.ProfilePicturePath,
            Roles = roles.ToList(),
        };
    }
}
