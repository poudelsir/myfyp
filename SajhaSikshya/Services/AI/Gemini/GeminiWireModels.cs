namespace SajhaSikshya.Services.AI.Gemini;

// Minimal wire-format subset of Google's Generative Language "generateContent" REST API
// (https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent) —
// only the fields GeminiAIService actually sends/reads, not the full API surface.
// Kept internal to this folder; nothing outside GeminiAIService should ever construct
// or parse these directly (see AIGenerationRequest/AIGenerationResult for the
// feature-facing shape).

internal sealed class GeminiGenerateContentRequest
{
    public required List<GeminiContent> Contents { get; init; }

    public GeminiGenerationConfig? GenerationConfig { get; init; }
}

internal sealed class GeminiContent
{
    public required List<GeminiPart> Parts { get; init; }
}

internal sealed class GeminiPart
{
    public required string Text { get; init; }
}

internal sealed class GeminiGenerationConfig
{
    public double? Temperature { get; init; }

    public int? MaxOutputTokens { get; init; }

    public string? ResponseMimeType { get; init; }

    public object? ResponseSchema { get; init; }
}

internal sealed class GeminiGenerateContentResponse
{
    public List<GeminiCandidate>? Candidates { get; init; }

    public GeminiUsageMetadata? UsageMetadata { get; init; }
}

internal sealed class GeminiCandidate
{
    public GeminiContentResponse? Content { get; init; }

    public string? FinishReason { get; init; }
}

internal sealed class GeminiContentResponse
{
    public List<GeminiPart>? Parts { get; init; }
}

internal sealed class GeminiUsageMetadata
{
    public int? TotalTokenCount { get; init; }
}
