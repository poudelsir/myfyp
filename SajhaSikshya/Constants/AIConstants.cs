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

    /// <summary>
    /// gemini-flash-latest is a "thinking" model whose internal reasoning tokens count
    /// against this same budget before the visible answer is written — a low cap was
    /// observed live to intermittently truncate a structured-JSON answer mid-object once
    /// thinking consumed most of it (the API returns 400 INVALID_ARGUMENT if thinking is
    /// explicitly disabled via thinkingConfig, so this model cannot opt out of thinking;
    /// giving it headroom instead is the only available fix). 1024 was too tight; 4096
    /// comfortably covers reasoning plus a full JSON or chat answer.
    /// </summary>
    public const int DefaultMaxOutputTokens = 4096;

    public const double DefaultTemperature = 0.6;

    /// <summary>How long an identical prompt's response is served from <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/> before a fresh Gemini call is made.</summary>
    public const int DefaultCacheDurationMinutes = 60;

    /// <summary>Error text stored on <see cref="Data.Entities.AI.AIUsageLog"/> is truncated to this length — enough for triage, never a full stack trace.</summary>
    public const int MaxErrorMessageLength = 500;

    /// <summary>Below this many comparable listings, the price prompt falls back to Gemini reasoning alone instead of citing marketplace data as a reference point.</summary>
    public const int MinComparableListingsForPricing = 3;

    /// <summary>Upper bound on how many comparable listing prices are pulled into the pricing prompt — enough signal without an unbounded prompt.</summary>
    public const int MaxComparableListingsSample = 20;

    /// <summary>Price recommendations aren't meaningful below this amount of description text — same 20-character floor <see cref="ListingConstants"/> already requires elsewhere for a listing's own Description field.</summary>
    public const int MinDescriptionLengthForPricing = 20;

    /// <summary>A chat question, not a document — keeps the Marketplace Assistant's input bounded well below <see cref="MaxPromptLength"/> even before grounding/history are added on top.</summary>
    public const int MaxAssistantQuestionLength = 500;

    /// <summary>"A short conversation history (last few exchanges only)" per Phase 10.3 — a rolling window, not an unbounded transcript. One exchange = one user message + one assistant reply.</summary>
    public const int MaxAssistantHistoryExchanges = 4;
}
