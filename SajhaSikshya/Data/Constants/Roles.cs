namespace SajhaSikshya.Data.Constants;

/// <summary>
/// Centralized, strongly-typed role names used throughout the application for
/// ASP.NET Core Identity authorization. Referencing these constants instead of
/// magic strings prevents typos in [Authorize(Roles = "...")] attributes and
/// keeps role management in a single, discoverable location.
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Student = "Student";

    /// <summary>
    /// All roles seeded into the database on application startup.
    /// Add new roles here so <see cref="Seed.DbInitializer"/> picks them up automatically.
    /// </summary>
    public static readonly IReadOnlyList<string> All = new[] { Admin, Student };
}
