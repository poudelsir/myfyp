using System.Text.Json;
using SajhaSikshya.Constants;
using SajhaSikshya.Data.Entities.Marketplace;
using SajhaSikshya.Data.Enums;
using SajhaSikshya.DTOs.AI;
using SajhaSikshya.Repositories.Interfaces;
using SajhaSikshya.Services.AI.Prompts;
using SajhaSikshya.Services.Interfaces.AI;

namespace SajhaSikshya.Services.AI;

/// <inheritdoc cref="IListingAIService"/>
public class ListingAIService : IListingAIService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IAIService _aiService;
    private readonly IUnitOfWork _unitOfWork;

    public ListingAIService(IAIService aiService, IUnitOfWork unitOfWork)
    {
        _aiService = aiService;
        _unitOfWork = unitOfWork;
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

    public async Task<ServiceResult<ListingPriceRecommendationDto>> RecommendPriceAsync(string userId, ListingPriceRecommendationRequest request)
    {
        if (request.IsDonation)
        {
            return ServiceResult<ListingPriceRecommendationDto>.Failure("Price recommendations aren't applicable to donation listings.");
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return ServiceResult<ListingPriceRecommendationDto>.Failure("Please enter a title before requesting a price recommendation.");
        }

        if (string.IsNullOrWhiteSpace(request.Description) || request.Description.Length < AIConstants.MinDescriptionLengthForPricing)
        {
            return ServiceResult<ListingPriceRecommendationDto>.Failure(
                $"Please write at least {AIConstants.MinDescriptionLengthForPricing} characters of description before requesting a price recommendation.");
        }

        var comparablePrices = await GetComparablePricesAsync(request.SubjectId, request.CategoryId, request.AcademicLevelId, request.ExcludeListingId);

        var promptInput = new PriceRecommendationPromptInput(
            request.Title,
            request.Description,
            request.Condition,
            request.CategoryName,
            request.SubjectName,
            request.AcademicLevelName,
            request.UniversityName,
            comparablePrices);

        var aiRequest = new AIGenerationRequest
        {
            Prompt = PriceRecommendationPromptBuilder.Build(promptInput),
            Feature = AIFeature.PriceRecommendation,
            PromptType = PriceRecommendationPromptBuilder.PromptType,
            UserId = userId,
            CacheKey = PriceRecommendationPromptBuilder.BuildCacheKey(promptInput),
            ResponseSchema = PriceRecommendationPromptBuilder.BuildResponseSchema(),
        };

        var result = await _aiService.GenerateAsync(aiRequest);
        if (!result.Succeeded || result.Data is null)
        {
            return ServiceResult<ListingPriceRecommendationDto>.Failure(result.Errors.ToArray());
        }

        ListingPriceRecommendationDto? recommendation;
        try
        {
            recommendation = JsonSerializer.Deserialize<ListingPriceRecommendationDto>(result.Data.Text, JsonOptions);
        }
        catch (JsonException)
        {
            recommendation = null;
        }

        if (recommendation is null || string.IsNullOrWhiteSpace(recommendation.Confidence))
        {
            return ServiceResult<ListingPriceRecommendationDto>.Failure("AI service returned an unexpected response. Please try again.");
        }

        return ServiceResult<ListingPriceRecommendationDto>.Success(recommendation);
    }

    /// <summary>
    /// Subject-level match first (same book/material) since that's the strongest price
    /// signal; if too few comparable rows exist, broadens to Category+AcademicLevel
    /// (same type of item at the same level) instead. Only Active/Reserved/Sold listings
    /// count as market signal — Draft/PendingApproval/Rejected/Archived aren't real
    /// prices, and donations are excluded since their price is always zero.
    /// </summary>
    private async Task<IReadOnlyList<decimal>> GetComparablePricesAsync(int subjectId, int categoryId, int academicLevelId, int? excludeListingId)
    {
        var repository = _unitOfWork.Repository<Listing>();

        var bySubject = await repository.FindProjectedAsync(
            l => l.SubjectId == subjectId && !l.IsDonation && l.Id != excludeListingId &&
                 (l.Status == ListingStatus.Active || l.Status == ListingStatus.Reserved || l.Status == ListingStatus.Sold),
            q => q.OrderByDescending(l => l.CreatedAtUtc),
            AIConstants.MaxComparableListingsSample,
            l => l.Price.Amount);

        if (bySubject.Count >= AIConstants.MinComparableListingsForPricing)
        {
            return bySubject;
        }

        var byCategory = await repository.FindProjectedAsync(
            l => l.CategoryId == categoryId && l.AcademicLevelId == academicLevelId && !l.IsDonation && l.Id != excludeListingId &&
                 (l.Status == ListingStatus.Active || l.Status == ListingStatus.Reserved || l.Status == ListingStatus.Sold),
            q => q.OrderByDescending(l => l.CreatedAtUtc),
            AIConstants.MaxComparableListingsSample,
            l => l.Price.Amount);

        return byCategory.Count >= AIConstants.MinComparableListingsForPricing ? byCategory : Array.Empty<decimal>();
    }
}
