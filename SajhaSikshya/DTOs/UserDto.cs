namespace SajhaSikshya.DTOs;

/// <summary>
/// Flat, presentation-safe projection of <see cref="Data.Entities.ApplicationUser"/>.
/// DTOs cross service/controller boundaries instead of entities so views and future
/// API endpoints never accidentally expose Identity internals (password hash,
/// security stamp, concurrency stamp, etc.).
/// </summary>
public class UserDto
{
    public string Id { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? ProfilePicturePath { get; set; }

    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
}
