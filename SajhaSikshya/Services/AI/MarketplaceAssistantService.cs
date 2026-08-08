using SajhaSikshya.Constants;
using SajhaSikshya.Data.Enums;
using SajhaSikshya.Services.AI.Prompts;
using SajhaSikshya.Services.Interfaces.AI;
using SajhaSikshya.Services.Interfaces.Catalog;
using SajhaSikshya.Services.Interfaces.Marketplace;
using SajhaSikshya.Services.Interfaces.Verification;

namespace SajhaSikshya.Services.AI;

/// <inheritdoc cref="IMarketplaceAssistantService"/>
public class MarketplaceAssistantService : IMarketplaceAssistantService
{
    private readonly IAIService _aiService;
    private readonly IListingQueryService _listingQueryService;
    private readonly ICategoryService _categoryService;
    private readonly IAcademicLevelService _academicLevelService;
    private readonly IUniversityService _universityService;
    private readonly IVerificationQueryService _verificationQueryService;

    public MarketplaceAssistantService(
        IAIService aiService,
        IListingQueryService listingQueryService,
        ICategoryService categoryService,
        IAcademicLevelService academicLevelService,
        IUniversityService universityService,
        IVerificationQueryService verificationQueryService)
    {
        _aiService = aiService;
        _listingQueryService = listingQueryService;
        _categoryService = categoryService;
        _academicLevelService = academicLevelService;
        _universityService = universityService;
        _verificationQueryService = verificationQueryService;
    }

    public async Task<ServiceResult<string>> AskAsync(string userId, string role, string question, IReadOnlyList<AssistantMessage> history)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return ServiceResult<string>.Failure("Please enter a question.");
        }

        var trimmedQuestion = question.Trim();
        if (trimmedQuestion.Length > AIConstants.MaxAssistantQuestionLength)
        {
            return ServiceResult<string>.Failure($"Please keep your question under {AIConstants.MaxAssistantQuestionLength} characters.");
        }

        var stats = await _listingQueryService.GetModerationStatsAsync();
        var categories = await _categoryService.GetEligibleParentCategoriesAsync(excludeCategoryId: null);
        var academicLevels = await _academicLevelService.GetAllActiveAsync();
        var universities = await _universityService.GetAllActiveAsync();
        var isVerified = await _verificationQueryService.IsUserVerifiedAsync(userId);

        var input = new MarketplaceAssistantPromptInput(
            trimmedQuestion,
            history,
            isVerified,
            role,
            stats.ApprovedCount,
            stats.TotalListings,
            stats.DonationCount,
            categories.Select(c => c.Name).ToList(),
            academicLevels.Select(a => a.Name).ToList(),
            universities.Select(u => u.Name).ToList());

        var request = new AIGenerationRequest
        {
            Prompt = MarketplaceAssistantPromptBuilder.Build(input),
            Feature = AIFeature.MarketplaceAssistant,
            PromptType = MarketplaceAssistantPromptBuilder.PromptType,
            UserId = userId,
            CacheKey = MarketplaceAssistantPromptBuilder.BuildCacheKey(input),
        };

        var result = await _aiService.GenerateAsync(request);
        if (!result.Succeeded || result.Data is null)
        {
            return ServiceResult<string>.Failure(result.Errors.ToArray());
        }

        return ServiceResult<string>.Success(result.Data.Text.Trim());
    }
}
