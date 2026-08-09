using System.Text.Json;
using SajhaSikshya.Data.Entities.AI;
using SajhaSikshya.Data.Entities.Marketplace;
using SajhaSikshya.Data.Entities.Notifications;
using SajhaSikshya.Data.Entities.Reviews;
using SajhaSikshya.Data.Enums;
using SajhaSikshya.DTOs.AI;
using SajhaSikshya.Repositories.Interfaces;
using SajhaSikshya.Services.AI.Prompts;
using SajhaSikshya.Services.Interfaces.AI;
using SajhaSikshya.Services.Interfaces.Catalog;
using SajhaSikshya.Services.Interfaces.Orders;
using SajhaSikshya.Services.Interfaces.Reviews;
using SajhaSikshya.Services.Interfaces.Verification;

namespace SajhaSikshya.Services.AI;

/// <inheritdoc cref="IAdminInsightsService"/>
public class AdminInsightsService : IAdminInsightsService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IAIService _aiService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrderQueryService _orderQueryService;
    private readonly IVerificationQueryService _verificationQueryService;
    private readonly IReviewQueryService _reviewQueryService;
    private readonly ICategoryService _categoryService;
    private readonly IUniversityService _universityService;

    public AdminInsightsService(
        IAIService aiService,
        IUnitOfWork unitOfWork,
        IOrderQueryService orderQueryService,
        IVerificationQueryService verificationQueryService,
        IReviewQueryService reviewQueryService,
        ICategoryService categoryService,
        IUniversityService universityService)
    {
        _aiService = aiService;
        _unitOfWork = unitOfWork;
        _orderQueryService = orderQueryService;
        _verificationQueryService = verificationQueryService;
        _reviewQueryService = reviewQueryService;
        _categoryService = categoryService;
        _universityService = universityService;
    }

    public async Task<AdminInsightsStatsDto> GetStatsAsync()
    {
        var now = DateTime.UtcNow;
        var thisMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastMonthStart = thisMonthStart.AddMonths(-1);

        var listingRepo = _unitOfWork.Repository<Listing>();
        var reviewRepo = _unitOfWork.Repository<Review>();
        var notificationRepo = _unitOfWork.Repository<Notification>();
        var aiLogRepo = _unitOfWork.Repository<AIUsageLog>();

        var orderStats = await _orderQueryService.GetStatisticsAsync();
        var verificationStats = await _verificationQueryService.GetStatisticsAsync();
        var reportedReviews = await _reviewQueryService.GetAdminQueueAsync(reportedOnly: true, pageNumber: 1, pageSize: 1);

        return new AdminInsightsStatsDto
        {
            TotalListings = await listingRepo.CountAsync(),
            ActiveListings = await listingRepo.CountAsync(l => l.Status == ListingStatus.Active),
            PendingListings = await listingRepo.CountAsync(l => l.Status == ListingStatus.PendingApproval),
            DonationListings = await listingRepo.CountAsync(l => l.IsDonation),
            ListingsThisMonth = await listingRepo.CountAsync(l => l.CreatedAtUtc >= thisMonthStart),
            ListingsLastMonth = await listingRepo.CountAsync(l => l.CreatedAtUtc >= lastMonthStart && l.CreatedAtUtc < thisMonthStart),
            DonationsThisMonth = await listingRepo.CountAsync(l => l.IsDonation && l.CreatedAtUtc >= thisMonthStart),
            DonationsLastMonth = await listingRepo.CountAsync(l => l.IsDonation && l.CreatedAtUtc >= lastMonthStart && l.CreatedAtUtc < thisMonthStart),
            TopCategories = await GetTopCategoriesAsync(listingRepo),
            TopUniversities = await GetTopUniversitiesAsync(listingRepo),

            TotalOrders = orderStats.TotalOrders,
            CompletedOrders = orderStats.CompletedCount,
            OrdersThisMonth = await _unitOfWork.Repository<Data.Entities.Orders.Order>().CountAsync(o => o.CreatedAtUtc >= thisMonthStart),
            OrdersLastMonth = await _unitOfWork.Repository<Data.Entities.Orders.Order>().CountAsync(o => o.CreatedAtUtc >= lastMonthStart && o.CreatedAtUtc < thisMonthStart),

            PendingVerifications = verificationStats.PendingCount,
            ApprovedVerifications = verificationStats.ApprovedCount,
            RejectedVerifications = verificationStats.RejectedCount,
            VerificationApprovalRatePercent = verificationStats.ApprovalRatePercent,

            TotalReviews = await reviewRepo.CountAsync(),
            AverageRating = await ComputePlatformAverageRatingAsync(reviewRepo),
            ReportedReviews = reportedReviews.TotalCount,

            TotalNotifications = await notificationRepo.CountAsync(),
            NotificationsThisMonth = await notificationRepo.CountAsync(n => n.CreatedAtUtc >= thisMonthStart),

            TotalAICalls = await aiLogRepo.CountAsync(),
            SuccessfulAICalls = await aiLogRepo.CountAsync(l => l.Success),
            CacheHitCount = await aiLogRepo.CountAsync(l => l.FromCache),
            AverageAIResponseTimeMs = await ComputeAverageAIResponseTimeAsync(aiLogRepo),
        };
    }

    public async Task<ServiceResult<AdminInsightsSummaryDto>> GenerateSummaryAsync(string adminUserId)
    {
        var stats = await GetStatsAsync();

        var request = new AIGenerationRequest
        {
            Prompt = AdminInsightsPromptBuilder.Build(stats),
            Feature = AIFeature.AdminInsights,
            PromptType = AdminInsightsPromptBuilder.PromptType,
            UserId = adminUserId,
            CacheKey = AdminInsightsPromptBuilder.BuildCacheKey(stats),
            ResponseSchema = AdminInsightsPromptBuilder.BuildResponseSchema(),
        };

        var result = await _aiService.GenerateAsync(request);
        if (!result.Succeeded || result.Data is null)
        {
            return ServiceResult<AdminInsightsSummaryDto>.Failure(result.Errors.ToArray());
        }

        AdminInsightsSummaryDto? summary;
        try
        {
            summary = JsonSerializer.Deserialize<AdminInsightsSummaryDto>(result.Data.Text, JsonOptions);
        }
        catch (JsonException)
        {
            summary = null;
        }

        if (summary is null || string.IsNullOrWhiteSpace(summary.Summary))
        {
            return ServiceResult<AdminInsightsSummaryDto>.Failure("AI service returned an unexpected response. Please try again.");
        }

        return ServiceResult<AdminInsightsSummaryDto>.Success(summary);
    }

    /// <summary>One CountAsync per active category — same "several small bounded queries" precedent used throughout the project (e.g. Review reputation distribution) rather than a new group-by repository method for a single call site.</summary>
    /// <summary>
    /// One query for every Active listing's CategoryId, grouped in memory, rather than
    /// one CountAsync per category — with the marketplace taxonomy now ~65 categories
    /// deep (departments + subcategories), a per-category round trip here would scale
    /// linearly with the taxonomy size instead of staying flat.
    /// </summary>
    private async Task<IReadOnlyList<NameCountDto>> GetTopCategoriesAsync(IGenericRepository<Listing> listingRepo)
    {
        var categories = await _categoryService.GetEligibleParentCategoriesAsync(excludeCategoryId: null);
        var activeListings = await listingRepo.FindAsync(l => l.Status == ListingStatus.Active);
        var countsByCategoryId = activeListings
            .GroupBy(l => l.CategoryId)
            .ToDictionary(g => g.Key, g => g.Count());

        return categories
            .Select(category => new NameCountDto(category.Name, countsByCategoryId.GetValueOrDefault(category.Id)))
            .Where(c => c.Count > 0)
            .OrderByDescending(c => c.Count)
            .Take(5)
            .ToList();
    }

    /// <summary>Same batched-then-grouped shape as <see cref="GetTopCategoriesAsync"/>, and for the same reason.</summary>
    private async Task<IReadOnlyList<NameCountDto>> GetTopUniversitiesAsync(IGenericRepository<Listing> listingRepo)
    {
        var universities = await _universityService.GetAllActiveAsync();
        var activeListings = await listingRepo.FindAsync(l => l.Status == ListingStatus.Active);
        var countsByUniversityId = activeListings
            .Where(l => l.UniversityId.HasValue)
            .GroupBy(l => l.UniversityId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        return universities
            .Select(university => new NameCountDto(university.Name, countsByUniversityId.GetValueOrDefault(university.Id)))
            .Where(c => c.Count > 0)
            .OrderByDescending(c => c.Count)
            .Take(5)
            .ToList();
    }

    /// <summary>Same 5-CountAsync-per-star-then-derive-average shape as <c>ReviewQueryService.GetReputationAsync</c>, just platform-wide instead of per-user.</summary>
    private static async Task<double> ComputePlatformAverageRatingAsync(IGenericRepository<Review> reviewRepo)
    {
        var total = 0;
        var weightedSum = 0;
        for (var star = 1; star <= 5; star++)
        {
            var count = await reviewRepo.CountAsync(r => r.Rating == star);
            total += count;
            weightedSum += star * count;
        }

        return total == 0 ? 0 : Math.Round((double)weightedSum / total, 1);
    }

    /// <summary>Excludes cache hits (always ~0ms) so this reflects real Gemini latency, not a mix diluted by instant cached responses.</summary>
    private static async Task<double> ComputeAverageAIResponseTimeAsync(IGenericRepository<AIUsageLog> aiLogRepo)
    {
        var times = await aiLogRepo.FindProjectedAsync(
            l => l.Success && !l.FromCache,
            q => q.OrderByDescending(l => l.CreatedAtUtc),
            500,
            l => l.ResponseTimeMs);

        return times.Count == 0 ? 0 : Math.Round(times.Average(), 0);
    }
}
