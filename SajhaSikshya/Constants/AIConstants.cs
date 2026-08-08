namespace SajhaSikshya.Constants;

/// <summary>
/// Business-rule constants for the AI layer (Phase 10) — bounds and durations that
/// aren't secrets, unlike <see cref="Configurations.GeminiSettings"/> which holds the
/// actual external-service configuration (API key, model, endpoint).
/// </summary>
public static class AIConstants
{
    /// <summary>Upper bound on a built prompt's length before it's rejected without ever reaching Gemini — a cheap guard against runaway input.</summary>
    public const int MaxPromptLength = 6000;

    public const int DefaultMaxOutputTokens = 1024;

    public const double DefaultTemperature = 0.6;

    /// <summary>How long an identical prompt's response is served from <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/> before a fresh Gemini call is made.</summary>
    public const int DefaultCacheDurationMinutes = 60;

    /// <summary>Error text stored on <see cref="Data.Entities.AI.AIUsageLog"/> is truncated to this length — enough for triage, never a full stack trace.</summary>
    public const int MaxErrorMessageLength = 500;
}
