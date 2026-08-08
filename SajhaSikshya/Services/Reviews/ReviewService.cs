using Microsoft.AspNetCore.Identity;
using SajhaSikshya.Constants;
using SajhaSikshya.Data.Constants;
using SajhaSikshya.Data.Entities;
using SajhaSikshya.Data.Entities.Orders;
using SajhaSikshya.Data.Entities.Reviews;
using SajhaSikshya.Data.Enums;
using SajhaSikshya.Repositories.Interfaces;
using SajhaSikshya.Services.Interfaces.Notifications;
using SajhaSikshya.Services.Interfaces.Reviews;
using SajhaSikshya.Services.Notifications;

namespace SajhaSikshya.Services.Reviews;

/// <summary>
/// Implements <see cref="IReviewService"/>. A review's direction and reviewee are
/// always derived from the order itself (<see cref="DetermineDirection"/>), never
/// accepted as caller input — the one design choice that makes "arbitrary profile
/// reviews" structurally impossible rather than just discouraged. Every mutation that
/// should notify someone does so via <see cref="_notificationService"/> after its own
/// <see cref="IUnitOfWork.SaveChangesAsync"/> commits, the same discipline
/// <c>ChatService</c>/<c>NotificationService</c> itself already established.
/// </summary>
public class ReviewService : IReviewService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ReviewService> _logger;

    public ReviewService(
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        UserManager<ApplicationUser> userManager,
        ILogger<ReviewService> logger)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<ServiceResult<int>> CreateAsync(int orderId, string reviewerId, int rating, string? title, string? comment)
    {
        var validationError = ValidateContent(rating, title, comment);
        if (validationError is not null)
        {
            return ServiceResult<int>.Failure(validationError);
        }

        var order = await _unitOfWork.Repository<Order>().GetByIdAsync(orderId);
        if (order is null || order.Status != OrderStatus.Completed)
        {
            return ServiceResult<int>.Failure("Only completed orders can be reviewed.");
        }

        var direction = DetermineDirection(order, reviewerId);
        if (direction is null)
        {
            return ServiceResult<int>.Failure("You were not a party to this order.");
        }

        var (revieweeId, reviewType) = direction.Value;

        var reviewRepository = _unitOfWork.Repository<Review>();
        var alreadyReviewed = await reviewRepository.AnyAsync(r => r.OrderId == orderId && r.ReviewType == reviewType);
        if (alreadyReviewed)
        {
            return ServiceResult<int>.Failure("You have already reviewed this order.");
        }

        var review = new Review
        {
            OrderId = orderId,
            ReviewerId = reviewerId,
            RevieweeId = revieweeId,
            Rating = rating,
            Title = string.IsNullOrWhiteSpace(title) ? null : title.Trim(),
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            ReviewType = reviewType,
        };

        await reviewRepository.AddAsync(review);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Review {ReviewId} ({ReviewType}) created for order {OrderId} by {ReviewerId}", review.Id, reviewType, orderId, reviewerId);

        var reviewerName = await GetUserNameAsync(reviewerId);
        var (notificationTitle, notificationMessage) = NotificationTemplates.NewReviewReceived(reviewerName, rating);
        await _notificationService.CreateAsync(revieweeId, NotificationType.Review, notificationTitle, notificationMessage, ReviewLink(revieweeId, reviewType), createdBy: reviewerId);

        return ServiceResult<int>.Success(review.Id);
    }

    public async Task<ServiceResult> UpdateAsync(int reviewId, string reviewerId, int rating, string? title, string? comment)
    {
        var validationError = ValidateContent(rating, title, comment);
        if (validationError is not null)
        {
            return ServiceResult.Failure(validationError);
        }

        var repository = _unitOfWork.Repository<Review>();
        var review = await repository.GetByIdAsync(reviewId);
        if (review is null || review.ReviewerId != reviewerId)
        {
            return ServiceResult.Failure("Review not found.");
        }

        if (DateTime.UtcNow - review.CreatedAtUtc > TimeSpan.FromHours(ReviewConstants.EditWindowHours))
        {
            return ServiceResult.Failure($"Reviews can only be edited within {ReviewConstants.EditWindowHours} hours of posting.");
        }

        review.Rating = rating;
        review.Title = string.IsNullOrWhiteSpace(title) ? null : title.Trim();
        review.Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        review.IsEdited = true;

        repository.Update(review);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeleteAsync(int reviewId, string reviewerId)
    {
        var repository = _unitOfWork.Repository<Review>();
        var review = await repository.GetByIdAsync(reviewId);
        if (review is null || review.ReviewerId != reviewerId)
        {
            return ServiceResult.Failure("Review not found.");
        }

        repository.Remove(review);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> ReportAsync(int reviewId, string reporterId)
    {
        var repository = _unitOfWork.Repository<Review>();
        var review = await repository.GetByIdAsync(reviewId);
        if (review is null)
        {
            return ServiceResult.Failure("Review not found.");
        }

        if (review.ReviewerId == reporterId)
        {
            return ServiceResult.Failure("You cannot report your own review.");
        }

        review.ReportCount++;
        repository.Update(review);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Review {ReviewId} reported by {ReporterId} (report count now {ReportCount})", reviewId, reporterId, review.ReportCount);

        var revieweeName = await GetUserNameAsync(review.RevieweeId);
        var (title, message) = NotificationTemplates.ReviewReported(revieweeName);
        await _notificationService.CreateBroadcastAsync(NotificationType.Review, title, message, AdminReviewQueueLink, reporterId, targetRole: Roles.Admin);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> ModerateAsync(int reviewId, ReviewModerationAction action, string adminId, string? reason = null)
    {
        var repository = _unitOfWork.Repository<Review>();

        // Restore is the only action that needs to see a soft-deleted row — mirrors
        // ListingService.ModerateListingAsync's identical reasoning for the same shape
        // of problem.
        var review = action == ReviewModerationAction.Restore
            ? await repository.GetByIdIncludingDeletedAsync(reviewId)
            : await repository.GetByIdAsync(reviewId);

        if (review is null)
        {
            return ServiceResult.Failure("Review not found.");
        }

        switch (action)
        {
            case ReviewModerationAction.Remove:
                repository.Remove(review);
                break;

            case ReviewModerationAction.Restore:
                review.IsDeleted = false;
                repository.Update(review);
                break;

            case ReviewModerationAction.ResetReportCount:
                review.ReportCount = 0;
                repository.Update(review);
                break;

            default:
                return ServiceResult.Failure("Unrecognized moderation action.");
        }

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Review {ReviewId} moderated: {Action} by {AdminId}", reviewId, action, adminId);

        if (action == ReviewModerationAction.Remove)
        {
            var (title, message) = NotificationTemplates.ReviewRemoved(reason);
            await _notificationService.CreateAsync(review.ReviewerId, NotificationType.Review, title, message, null, createdBy: adminId);
        }

        return ServiceResult.Success();
    }

    /// <summary>Rating range, and the same "at least a rating, text is optional" content rule <see cref="CreateAsync"/>/<see cref="UpdateAsync"/> share — user-authored content is validated and rejected on failure, never silently truncated the way system-composed notification text is.</summary>
    private static string? ValidateContent(int rating, string? title, string? comment)
    {
        if (rating < ReviewConstants.MinimumRating || rating > ReviewConstants.MaximumRating)
        {
            return $"Rating must be between {ReviewConstants.MinimumRating} and {ReviewConstants.MaximumRating}.";
        }

        if (!string.IsNullOrEmpty(title) && title.Length > ReviewConstants.MaximumTitleLength)
        {
            return $"Title cannot exceed {ReviewConstants.MaximumTitleLength} characters.";
        }

        if (!string.IsNullOrEmpty(comment) && comment.Length > ReviewConstants.MaximumCommentLength)
        {
            return $"Comment cannot exceed {ReviewConstants.MaximumCommentLength} characters.";
        }

        return null;
    }

    /// <summary>Derives who is being reviewed and in which direction purely from the order and the caller's id — the structural guarantee that a review can never be "from" someone who wasn't actually a party to the transaction. Null if the caller was neither the buyer nor the seller.</summary>
    private static (string RevieweeId, ReviewType ReviewType)? DetermineDirection(Order order, string reviewerId)
    {
        if (reviewerId == order.BuyerId)
        {
            return (order.SellerId, ReviewType.BuyerToSeller);
        }

        if (reviewerId == order.SellerId)
        {
            return (order.BuyerId, ReviewType.SellerToBuyer);
        }

        return null;
    }

    private async Task<string> GetUserNameAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user?.FullName ?? "Someone";
    }

    private static string ReviewLink(string revieweeId, ReviewType reviewType) => $"/reviews/user/{revieweeId}?type={reviewType}";

    private const string AdminReviewQueueLink = "/Admin/Reviews";
}
