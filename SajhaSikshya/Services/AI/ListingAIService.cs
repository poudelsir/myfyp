using System.Text.Json;
using SajhaSikshya.Data.Enums;
using SajhaSikshya.DTOs.AI;
using SajhaSikshya.Services.AI.Prompts;
using SajhaSikshya.Services.Interfaces.AI;

namespace SajhaSikshya.Services.AI;

/// <inheritdoc cref="IListingAIService"/>
public class ListingAIService : IListingAIService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IAIService _aiService;

    public ListingAIService(IAIService aiService)
    {
        _aiService = aiService;
    }

    public async Task<ServiceResult<ListingDescriptionSuggestionDto>> GenerateDescriptionAsync(string userId, ListingDescriptionPromptInput input)
    {
        var request = new AIGenerationRequest
        {
            Prompt = ListingDescriptionPromptBuilder.Build(input),
            Feature = AIFeature.ListingDescriptionGenerator,
            PromptType = ListingDescriptionPromptBuilder.PromptType,
            UserId = userId,
            CacheKey = ListingDescriptionPromptBuilder.BuildCacheKey(input),
            ResponseSchema = ListingDescriptionPromptBuilder.BuildResponseSchema(),
        };

        var result = await _aiService.GenerateAsync(request);
        if (!result.Succeeded || result.Data is null)
        {
            return ServiceResult<ListingDescriptionSuggestionDto>.Failure(result.Errors.ToArray());
        }

        ListingDescriptionSuggestionDto? suggestion;
        try
        {
            suggestion = JsonSerializer.Deserialize<ListingDescriptionSuggestionDto>(result.Data.Text, JsonOptions);
        }
        catch (JsonException)
        {
            suggestion = null;
        }

        if (suggestion is null || string.IsNullOrWhiteSpace(suggestion.Title) || string.IsNullOrWhiteSpace(suggestion.Description))
        {
            return ServiceResult<ListingDescriptionSuggestionDto>.Failure("AI service returned an unexpected response. Please try again.");
        }

        return ServiceResult<ListingDescriptionSuggestionDto>.Success(suggestion);
    }
}
