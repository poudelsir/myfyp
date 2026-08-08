using SajhaSikshya.Extensions;

namespace SajhaSikshya.Services.AI.Prompts;

/// <summary>
/// Builds the prompt (and matching response schema / cache key) for the AI Listing
/// Description Generator. Stateless and pure — a static class rather than a
/// DI-registered service, since it needs nothing but its input to do its job; see
/// <see cref="ListingAIService"/> for where it's actually called from. This is the
/// "ListingDescriptionPrompt" builder called out in Phase 10's architectural review —
/// its whole point is keeping this prompt text in exactly one place instead of
/// inlined in a controller or service.
/// </summary>
public static class ListingDescriptionPromptBuilder
{
    public const string PromptType = "ListingDescription";

    public static string Build(ListingDescriptionPromptInput input)
    {
        var titleLine = string.IsNullOrWhiteSpace(input.Title)
            ? "Not provided — invent a clear, accurate title from the facts below."
            : input.Title!;

        var priceLine = input.IsDonation ? "Free (donation)" : $"NPR {input.PriceAmount:0.##}";

        return $"""
            You are a copywriter for SajhaSikshya, a marketplace where Nepali university
            students buy, sell, and donate secondhand textbooks and study materials to
            each other. Write a listing based only on these facts — do not invent an
            edition, ISBN, page count, or author that wasn't given to you.

            Seller's working title: {titleLine}
            Condition: {input.Condition.GetDescription()}
            Subject: {input.SubjectName}
            Academic level: {input.AcademicLevelName}
            Category: {input.CategoryName}
            Price: {priceLine}

            Write:
            - title: A concise, honest listing title under 80 characters. Include the
              subject if the seller's working title doesn't already mention it.
            - description: 2-4 plain-text sentences covering the condition and who it
              would be useful for, in a friendly, trustworthy tone for a student buyer.
              No markdown, no emojis.
            - keywords: 5-8 short, lowercase search terms a buyer might type to find
              this item.

            Respond with JSON matching the provided schema only.
            """;
    }

    /// <summary>Gemini's OpenAPI-subset schema format (uppercase type names) — forces a parseable JSON response instead of free text GeminiAIService/ListingAIService would need to regex apart.</summary>
    public static object BuildResponseSchema() => new
    {
        type = "OBJECT",
        properties = new
        {
            title = new { type = "STRING" },
            description = new { type = "STRING" },
            keywords = new { type = "ARRAY", items = new { type = "STRING" } },
        },
        required = new[] { "title", "description", "keywords" },
    };

    /// <summary>Every field that changes the prompt text is part of the key, so a cache hit only ever happens for a genuinely identical request.</summary>
    public static string BuildCacheKey(ListingDescriptionPromptInput input) =>
        $"ai:listingdesc:{input.Title}:{input.Condition}:{input.CategoryName}:{input.SubjectName}:{input.AcademicLevelName}:{input.PriceAmount}:{input.IsDonation}";
}
