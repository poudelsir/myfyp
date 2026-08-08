using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SajhaSikshya.Data.Entities.Verification;
using SajhaSikshya.Data.Enums;
using SajhaSikshya.DTOs;
using SajhaSikshya.DTOs.Verification;
using SajhaSikshya.Mappings.Verification;
using SajhaSikshya.Repositories.Interfaces;
using SajhaSikshya.Services.Interfaces.Verification;

namespace SajhaSikshya.Services.Verification;

public class VerificationQueryService : IVerificationQueryService
{
    private readonly IUnitOfWork _unitOfWork;

    public VerificationQueryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<VerificationDto?> GetCurrentStatusAsync(string userId)
    {
        var repository = _unitOfWork.Repository<StudentVerification>();

        // "Most recent row" via GetPagedAsync(pageSize: 1) rather than a bare
        // FirstOrDefaultAsync — the repository's FirstOrDefaultAsync has no orderBy
        // parameter, and relying on undefined row order to mean "newest" would be wrong.
        var page = await repository.GetPagedAsync(
            1,
            1,
            filter: v => v.UserId == userId,
            orderBy: q => q.OrderByDescending(v => v.SubmittedAtUtc),
            include: IncludeVerificationDetails);

        return page.Items.FirstOrDefault()?.ToDto();
    }

    public async Task<PagedResult<VerificationDto>> GetHistoryAsync(string userId, int pageNumber, int pageSize)
    {
        var repository = _unitOfWork.Repository<StudentVerification>();
        var page = await repository.GetPagedAsync(
            pageNumber,
            pageSize,
            filter: v => v.UserId == userId,
            orderBy: q => q.OrderByDescending(v => v.SubmittedAtUtc),
            include: IncludeVerificationDetails);

        return ToPagedDto(page);
    }

    public async Task<PagedResult<VerificationDto>> GetQueueAsync(VerificationStatus? status, string? searchTerm, int pageNumber, int pageSize)
    {
        var repository = _unitOfWork.Repository<StudentVerification>();

        // Precompute outside the expression tree — same safe pattern already used by
        // ListingQueryService.GetAllForAdminAsync for the identical reason (calling
        // string.IsNullOrWhiteSpace *inside* an Expression<Func<T,bool>> risks EF Core
        // translation issues).
        var hasSearchTerm = !string.IsNullOrWhiteSpace(searchTerm);

        Expression<Func<StudentVerification, bool>> filter = v =>
            (!status.HasValue || v.Status == status.Value)
            && (!hasSearchTerm || v.StudentNumber.Contains(searchTerm!) || (v.User.FirstName + " " + v.User.LastName).Contains(searchTerm!));

        var page = await repository.GetPagedAsync(
            pageNumber,
            pageSize,
            filter: filter,
            orderBy: q => q.OrderByDescending(v => v.SubmittedAtUtc),
            include: IncludeVerificationDetails);

        return ToPagedDto(page);
    }

    public async Task<VerificationDetailDto?> GetForReviewAsync(int verificationId)
    {
        var repository = _unitOfWork.Repository<StudentVerification>();
        var verification = await repository.FirstOrDefaultAsync(v => v.Id == verificationId, IncludeVerificationDetails);
        return verification?.ToDetailDto();
    }

    public async Task<VerificationStatisticsDto> GetStatisticsAsync()
    {
        var repository = _unitOfWork.Repository<StudentVerification>();

        var pendingCount = await repository.CountAsync(v => v.Status == VerificationStatus.Pending);
        var approvedCount = await repository.CountAsync(v => v.Status == VerificationStatus.Verified);
        var rejectedCount = await repository.CountAsync(v => v.Status == VerificationStatus.Rejected);

        var reviewedCount = approvedCount + rejectedCount;
        var approvalRate = reviewedCount == 0 ? 0 : Math.Round(approvedCount * 100.0 / reviewedCount, 1);

        // Just the two timestamps needed for the average — not the full entity — for
        // every reviewed request. Averaged in memory since EF Core/SQL Server has no
        // direct "average of a computed TimeSpan" translation; the row count here is
        // bounded by how many requests have ever been reviewed, which is small enough
        // for a low-frequency admin dashboard metric.
        var reviewDurations = await repository.FindProjectedAsync(
            filter: v => v.ReviewedAtUtc != null,
            orderBy: q => q.OrderByDescending(v => v.ReviewedAtUtc),
            take: int.MaxValue,
            selector: v => new { v.SubmittedAtUtc, v.ReviewedAtUtc });

        var averageHours = reviewDurations.Count == 0
            ? (double?)null
            : Math.Round(reviewDurations.Average(v => (v.ReviewedAtUtc!.Value - v.SubmittedAtUtc).TotalHours), 1);

        var recentPage = await repository.GetPagedAsync(
            1,
            5,
            filter: v => v.ReviewedAtUtc != null,
            orderBy: q => q.OrderByDescending(v => v.ReviewedAtUtc),
            include: IncludeVerificationDetails);

        return new VerificationStatisticsDto
        {
            PendingCount = pendingCount,
            ApprovedCount = approvedCount,
            RejectedCount = rejectedCount,
            ApprovalRatePercent = approvalRate,
            AverageReviewTimeHours = averageHours,
            RecentReviews = recentPage.Items.Select(v => v.ToDto()).ToList(),
        };
    }

    public async Task<VerificationImageAccessDto?> GetImageAccessAsync(int verificationId)
    {
        var repository = _unitOfWork.Repository<StudentVerification>();

        // Projects straight to the two fields needed for the authorization check and
        // file lookup — never loads the full entity just to read a path.
        var results = await repository.FindProjectedAsync(
            filter: v => v.Id == verificationId,
            orderBy: q => q.OrderBy(v => v.Id),
            take: 1,
            selector: v => new VerificationImageAccessDto { UserId = v.UserId, ImagePath = v.StudentIdImagePath });

        return results.FirstOrDefault();
    }

    public async Task<bool> IsUserVerifiedAsync(string userId)
    {
        var repository = _unitOfWork.Repository<StudentVerification>();
        return await repository.AnyAsync(v => v.UserId == userId && v.Status == VerificationStatus.Verified);
    }

    public async Task<IReadOnlySet<string>> GetVerifiedUserIdsAsync(IEnumerable<string> userIds)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new HashSet<string>();
        }

        var repository = _unitOfWork.Repository<StudentVerification>();
        var verifiedIds = await repository.FindProjectedAsync(
            filter: v => v.Status == VerificationStatus.Verified && ids.Contains(v.UserId),
            orderBy: q => q.OrderBy(v => v.UserId),
            take: ids.Count,
            selector: v => v.UserId);

        return verifiedIds.ToHashSet();
    }

    private static PagedResult<VerificationDto> ToPagedDto(PagedResult<StudentVerification> page) => new()
    {
        Items = page.Items.Select(v => v.ToDto()).ToList(),
        PageNumber = page.PageNumber,
        PageSize = page.PageSize,
        TotalCount = page.TotalCount,
    };

    private static IQueryable<StudentVerification> IncludeVerificationDetails(IQueryable<StudentVerification> query) => query
        .Include(v => v.User)
        .Include(v => v.University)
        .Include(v => v.ReviewedByUser);
}
