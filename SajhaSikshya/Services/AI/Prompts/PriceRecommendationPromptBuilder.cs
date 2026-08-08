using SajhaSikshya.Extensions;

namespace SajhaSikshya.Services.AI.Prompts;

/// <summary>
/// Builds the prompt, response schema, and cache key for the AI Price Recommendation
/// feature — deliberately separate from <see cref="ListingDescriptionPromptBuilder"/>
/// (Phase 10's "dedicated prompt builder for pricing" requirement) since the two
/// features need entirely different facts, instructions, and output shapes; sharing
/// one builder between them would mean branching on feature inside a single class
/// instead of two small, independently readable ones.
/// </summary>
public static class PriceRecommendationPromptBuilder
{
    public const string PromptType = "PriceRecommendation";

    public static string Build(PriceRecommendationPromptInput input)
    {
        var universityLine = string.IsNullOrWhiteSpace(input.UniversityName) ? "Not specified" : input.UniversityName!;

        var historicalSection = input.ComparablePrices.Count == 0
            ? """
              No comparable listings are currently available on the marketplace for this
              subject or category. Base your recommendation on general knowledge of
              secondhand textbook and study material pricing in Nepal for this type of
              item and condition, and reflect that missing market data in a lower
              confidence level.
              """
            : $"""
               Comparable listings currently on the marketplace for this subject/category
               (NPR): {string.Join(", ", input.ComparablePrices.Select(p => p.ToString("0.##")))}
               Use these as a strong reference point for your recommendation, and reflect
               how consistent or scattered they are in your confidence level.
               """;

        return $"""
            You are a pricing analyst for SajhaSikshya, a marketplace where Nepali
            university students buy and sell secondhand textbooks and study materials to
            each other. Recommend a fair, competitive selling price in NPR based only on
            the facts below — do not invent details not given to you.

            Title: {input.Title}
            Description: {input.Description}
            Condition: {input.Condition.GetDescription()}
            Subject: {input.SubjectName}
            Academic level: {input.AcademicLevelName}
            Category: {input.CategoryName}
            University: {universityLine}

            {historicalSection}

            Provide:
            - suggestedPrice: A single fair price in NPR.
            - suggestedMinPrice: The lower bound of a reasonable price range.
            - suggestedMaxPrice: The upper bound of a reasonable price range.
            - confidence: "Low", "Medium", or "High" — High only when comparable
              marketplace listings were provided and consistent, Low when reasoning
              without comparable data or with widely inconsistent comparables.
            - explanation: 1-2 plain-text sentences justifying the recommendation.

            Respond with JSON matching the provided schema only.
            """;
    }

    public static object BuildResponseSchema() => new
    {
        type = "OBJECT",
        properties = new
        {
            suggestedPrice = new { type = "NUMBER" },
            suggestedMinPrice = new { type = "NUMBER" },
            suggestedMaxPrice = new { type = "NUMBER" },
            confidence = new { type = "STRING", @enum = new[] { "Low", "Medium", "High" } },
            explanation = new { type = "STRING" },
        },
        required = new[] { "suggestedPrice", "suggestedMinPrice", "suggestedMaxPrice", "confidence", "explanation" },
    };

    /// <summary>
    /// Every field that changes the prompt text is part of the key. Description is
    /// reduced to its length + hash code rather than included verbatim — descriptions
    /// can be long free text, and a stable-enough fingerprint is all a cache key needs
    /// (this cache never needs to survive a process restart, so a non-cryptographic
    /// hash is fine). Comparable prices are included as-is, so a genuinely changed
    /// marketplace data set correctly misses the cache instead of serving a stale range.
    /// </summary>
    public static string BuildCacheKey(PriceRecommendationPromptInput input) =>
        $"ai:price:{input.Title}:{input.Description.Length}:{input.Description.GetHashCode()}:{input.Condition}:" +
        $"{input.CategoryName}:{input.SubjectName}:{input.AcademicLevelName}:{input.UniversityName}:" +
        $"{string.Join(",", input.ComparablePrices)}";
}
