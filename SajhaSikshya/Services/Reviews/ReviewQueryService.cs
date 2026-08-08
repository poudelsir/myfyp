using Microsoft.EntityFrameworkCore;
using SajhaSikshya.Data.Entities.Reviews;
using SajhaSikshya.Data.Enums;
using SajhaSikshya.DTOs;
using SajhaSikshya.DTOs.Reviews;
using SajhaSikshya.Mappings.Reviews;
using SajhaSikshya.Repositories.Interfaces;
using SajhaSikshya.Services.Interfaces.Reviews;

namespace SajhaSikshya.Services.Reviews;

public class ReviewQueryService : IReviewQueryService
{
    private readonly IUnitOfWork _unitOfWork;

    public ReviewQueryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<ReviewDto>> GetReviewsForUserAsync(string revieweeId, ReviewType reviewType, int? ratingFilter, int pageNumber, int pageSize)
    {
        var repository = _unitOfWork.Repository<Review>();
        var page = await repository.GetPagedAsync(
            pageNumber,
            pageSize,
            filter: r => r.RevieweeId == revieweeId && r.ReviewType == reviewType && (!ratingFilter.HasValue || r.Rating == ratingFilter.Value),
            orderBy: q => q.OrderByDescending(r => r.CreatedAtUtc),
            include: IncludeReviewDetails);

        return ToPagedDto(page);
    }

    public async Task<PagedResult<ReviewDto>> GetMyReviewsAsync(string reviewerId, int pageNumber, int pageSize)
    {
        var repository = _unitOfWork.Repository<Review>();
        var page = await repository.GetPagedAsync(
            pageNumber,
            pageSize,
            filter: r => r.ReviewerId == reviewerId,
            orderBy: q => q.OrderByDescending(r => r.CreatedAtUtc),
            include: IncludeReviewDetails);

        return ToPagedDto(page);
    }

    public async Task<ReviewDto?> GetReviewForOrderAsync(int orderId, ReviewType reviewType)
    {
        var repository = _unitOfWork.Repository<Review>();
        var review = await repository.FirstOrDefaultAsync(r => r.OrderId == orderId && r.ReviewType == reviewType, IncludeReviewDetails);
        return review?.ToDto();
    }

    public async Task<ReviewDto?> GetByIdAsync(int reviewId)
    {
        var repository = _unitOfWork.Repository<Review>();
        var review = await repository.FirstOrDefaultAsync(r => r.Id == reviewId, IncludeReviewDetails);
        return review?.ToDto();
    }

    /// <summary>
    /// Computed live from the current, non-deleted review rows rather than a maintained
    /// counter denormalized onto <c>ApplicationUser</c> — deliberately rejecting the
    /// spec's own suggestion to "consider" that. Reviews are created only after a
    /// completed order (itself a rate-limited event, not a hot path like a listing page
    /// view), so a handful of indexed COUNT queries per profile load is cheap and, more
    /// importantly, can never drift out of sync the way a maintained counter could if an
    /// update path were ever missed. Five bounded COUNT calls per direction (one per
    /// star rating) — the same "several small bounded queries" tradeoff
    /// <c>OrderQueryService.GetStatisticsAsync</c> already established — also yields the
    /// total and average for free (summed/weighted from the same five numbers) without
    /// a sixth query.
    /// </summary>
    public async Task<ReputationDto> GetReputationAsync(string userId)
    {
        var repository = _unitOfWork.Repository<Review>();

        var sellerDistribution = await GetRatingDistributionAsync(repository, userId, ReviewType.BuyerToSeller);
        var buyerDistribution = await GetRatingDistributionAsync(repository, userId, ReviewType.SellerToBuyer);

        return new ReputationDto
        {
            SellerRatingDistribution = sellerDistribution,
            SellerReviewCount = sellerDistribution.Sum(),
            SellerAverageRating = ComputeAverage(sellerDistribution),
            BuyerRatingDistribution = buyerDistribution,
            BuyerReviewCount = buyerDistribution.Sum(),
            BuyerAverageRating = ComputeAverage(buyerDistribution),
        };
    }

    public async Task<PagedResult<ReviewDto>> GetAdminQueueAsync(bool reportedOnly, int pageNumber, int pageSize)
    {
        var repository = _unitOfWork.Repository<Review>();

        if (reportedOnly)
        {
            var page = await repository.GetPagedAsync(
                pageNumber,
                pageSize,
                filter: r => r.ReportCount > 0,
                orderBy: q => q.OrderByDescending(r => r.ReportCount).ThenByDescending(r => r.CreatedAtUtc),
                include: IncludeReviewDetails);

            return ToPagedDto(page);
        }

        // Includes previously-removed reviews (IsDeleted = true) so an admin can find
        // and Restore one — GetPagedAsync alone would filter those out entirely.
        var allPage = await repository.GetPagedIncludingDeletedAsync(
            pageNumber,
            pageSize,
            filter: null,
            orderBy: q => q.OrderByDescending(r => r.CreatedAtUtc),
            include: IncludeReviewDetails);

        return ToPagedDto(allPage);
    }

    private static async Task<int[]> GetRatingDistributionAsync(IGenericRepository<Review> repository, string revieweeId, ReviewType reviewType)
    {
        var distribution = new int[5];
        for (var star = 1; star <= 5; star++)
        {
            var starValue = star;
            distribution[star - 1] = await repository.CountAsync(r =>
                r.RevieweeId == revieweeId && r.ReviewType == reviewType && r.Rating == starValue);
        }

        return distribution;
    }

    private static double ComputeAverage(IReadOnlyList<int> distribution)
    {
        var total = distribution.Sum();
        if (total == 0)
        {
            return 0;
        }

        var weightedSum = 0;
        for (var i = 0; i < distribution.Count; i++)
        {
            weightedSum += (i + 1) * distribution[i];
        }

        return Math.Round((double)weightedSum / total, 1);
    }

    private static PagedResult<ReviewDto> ToPagedDto(PagedResult<Review> page) => new()
    {
        Items = page.Items.Select(r => r.ToDto()).ToList(),
        PageNumber = page.PageNumber,
        PageSize = page.PageSize,
        TotalCount = page.TotalCount,
    };

    /// <summary>Enough to render a review card — reviewer/reviewee names+avatars, and the order's listing title/reference for context.</summary>
    private static IQueryable<Review> IncludeReviewDetails(IQueryable<Review> query) => query
        .Include(r => r.Reviewer)
        .Include(r => r.Reviewee)
        .Include(r => r.Order)
        .ThenInclude(o => o.Listing);
}
