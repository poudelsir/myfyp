namespace SajhaSikshya.Configurations;

/// <summary>
/// Strongly-typed Gemini API settings bound from the "Gemini" section of
/// appsettings.json (Options pattern — same convention as <see cref="EmailSettings"/>).
/// <see cref="ApiKey"/> is never checked into appsettings.json (it ships empty there);
/// it's set via `dotnet user-secrets` in Development and an environment variable /
/// secret store in Production, so it never lives in source control.
/// </summary>
public class GeminiSettings
{
    public const string SectionName = "Gemini";

    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "gemini-flash-latest";

    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/";

    public int TimeoutSeconds { get; set; } = 30;
}
