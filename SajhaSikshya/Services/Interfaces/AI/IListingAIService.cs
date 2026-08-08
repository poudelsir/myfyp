using SajhaSikshya.DTOs.AI;
using SajhaSikshya.Services.AI;
using SajhaSikshya.Services.AI.Prompts;

namespace SajhaSikshya.Services.Interfaces.AI;

/// <summary>Feature-facing AI service for the Listing module — the thin layer between the Student area's ListingsController and the central <see cref="IAIService"/>.</summary>
public interface IListingAIService
{
    Task<ServiceResult<ListingDescriptionSuggestionDto>> GenerateDescriptionAsync(string userId, ListingDescriptionPromptInput input);

    Task<ServiceResult<ListingPriceRecommendationDto>> RecommendPriceAsync(string userId, ListingPriceRecommendationRequest request);
}
