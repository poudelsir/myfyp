using SajhaSikshya.Data.Enums;

namespace SajhaSikshya.Services.AI;

/// <summary>
/// A single, feature-agnostic Gemini call. Every AI feature (Listing Description
/// Generator today; Price Recommendation, Marketplace Assistant, Admin Insights in
/// later sub-phases) builds one of these via its own prompt builder and hands it to
/// <see cref="Interfaces.AI.IAIService"/> — the request already carries everything
/// <see cref="GeminiAIService"/> needs (what to log it as, whether to cache it, what
/// shape to demand back), so the central service never needs feature-specific branches.
/// </summary>
public class AIGenerationRequest
{
    public required string Prompt { get; init; }

    public required AIFeature Feature { get; init; }

    /// <summary>Identifies which prompt builder produced <see cref="Prompt"/> — stored on the usage log as-is.</summary>
    public required string PromptType { get; init; }

    public string? UserId { get; init; }

    /// <summary>
    /// When set, identical requests are served from <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/>
    /// for <see cref="Constants.AIConstants.DefaultCacheDurationMinutes"/> instead of
    /// calling Gemini again. Null opts a feature out of caching entirely (e.g. a future
    /// conversational Assistant where "the same question twice" isn't a meaningful
    /// concept to cache) — caching is per-caller-request, not a blanket policy.
    /// </summary>
    public string? CacheKey { get; init; }

    /// <summary>
    /// Gemini's OpenAPI-subset response schema object (e.g. <c>{ type: "OBJECT", properties: {...} }</c>),
    /// serialized as-is into <c>generationConfig.responseSchema</c> with
    /// <c>responseMimeType: "application/json"</c>. Null for free-text generation.
    /// </summary>
    public object? ResponseSchema { get; init; }

    public int? MaxOutputTokens { get; init; }

    public double? Temperature { get; init; }
}

/// <summary>Successful outcome of <see cref="Interfaces.AI.IAIService.GenerateAsync"/> — <see cref="Text"/> is the raw model output (a JSON string when <see cref="AIGenerationRequest.ResponseSchema"/> was set, otherwise plain text), left for the calling feature service to interpret.</summary>
public class AIGenerationResult
{
    public required string Text { get; init; }

    public int? TokenCount { get; init; }

    public long ResponseTimeMs { get; init; }

    public bool FromCache { get; init; }
}
