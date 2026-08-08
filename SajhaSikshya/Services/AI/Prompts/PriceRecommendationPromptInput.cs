using SajhaSikshya.Data.Enums;

namespace SajhaSikshya.Services.AI.Prompts;

/// <summary>Everything <see cref="PriceRecommendationPromptBuilder"/> needs. <see cref="ComparablePrices"/> is empty when fewer than <see cref="Constants.AIConstants.MinComparableListingsForPricing"/> matching listings exist — the builder falls back to reasoning-only wording in that case rather than the caller needing to branch.</summary>
public record PriceRecommendationPromptInput(
    string Title,
    string Description,
    BookCondition Condition,
    string CategoryName,
    string SubjectName,
    string AcademicLevelName,
    string? UniversityName,
    IReadOnlyList<decimal> ComparablePrices);
