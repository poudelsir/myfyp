namespace SajhaSikshya.Configurations;

/// <summary>
/// General application-wide settings bound from the "ApplicationSettings" section
/// of appsettings.json. Centralizes values that would otherwise be scattered as
/// magic strings/numbers across controllers and views.
/// </summary>
public class ApplicationSettings
{
    public const string SectionName = "ApplicationSettings";

    public string ApplicationName { get; set; } = "SajhaSikshya";

    public string SupportEmail { get; set; } = string.Empty;

    /// <summary>
    /// Maximum profile picture upload size, in bytes, enforced by
    /// <see cref="Validators.AllowedFileExtensionsAttribute"/> callers.
    /// </summary>
    public long MaxUploadSizeBytes { get; set; } = 5 * 1024 * 1024;
}
