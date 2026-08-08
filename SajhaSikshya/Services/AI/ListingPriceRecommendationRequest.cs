using SajhaSikshya.Data.Enums;

namespace SajhaSikshya.Services.AI;

/// <summary>
/// What <see cref="Interfaces.AI.IListingAIService.RecommendPriceAsync"/> needs from its
/// caller — Category/Subject/AcademicLevel/University names already resolved (same
/// convention as <see cref="Prompts.ListingDescriptionPromptInput"/>), plus the raw
/// CategoryId/SubjectId/AcademicLevelId the service needs to look up comparable listings
/// and an optional listing id to exclude (editing a listing shouldn't compare it against itself).
/// </summary>
public record ListingPriceRecommendationRequest(
    string Title,
    string Description,
    BookCondition Condition,
    int CategoryId,
    string CategoryName,
    int SubjectId,
    string SubjectName,
    int AcademicLevelId,
    string AcademicLevelName,
    string? UniversityName,
    bool IsDonation,
    int? ExcludeListingId);
