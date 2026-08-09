using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SajhaSikshya.Data.Constants;
using SajhaSikshya.Data.Entities;
using SajhaSikshya.Data.Entities.AI;
using SajhaSikshya.Data.Entities.Chat;
using SajhaSikshya.Data.Entities.Marketplace;
using SajhaSikshya.Data.Entities.Orders;
using SajhaSikshya.Data.Entities.Reviews;
using SajhaSikshya.Data.Entities.Verification;
using SajhaSikshya.Data.Enums;
using SajhaSikshya.DTOs.AI;
using SajhaSikshya.DTOs.Dashboard;
using SajhaSikshya.DTOs.Marketplace;
using SajhaSikshya.DTOs.Verification;
using SajhaSikshya.Extensions;
using SajhaSikshya.Repositories.Interfaces;
using SajhaSikshya.Services.Interfaces.AI;
using SajhaSikshya.Services.Interfaces.Chat;
using SajhaSikshya.Services.Interfaces.Dashboard;
using SajhaSikshya.Services.Interfaces.Marketplace;
using SajhaSikshya.Services.Interfaces.Notifications;
using SajhaSikshya.Services.Interfaces.Orders;
using SajhaSikshya.Services.Interfaces.Reviews;
using SajhaSikshya.Services.Interfaces.Verification;
using SajhaSikshya.ViewModels.Marketplace;

namespace SajhaSikshya.Services.Dashboard;

/// <inheritdoc cref="IDashboardQueryService"/>
public class DashboardQueryService : IDashboardQueryService
{
    private const int RecommendedListingsCap = 6;
    private const int StudentRecentActivityCap = 10;
    private const int AdminRecentActivityCap = 15;
    private const int PerSourceActivityTake = 5;
    private const int RegistrationGrowthMonths = 6;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IListingQueryService _listingQueryService;
    private readonly IListingSearchService _listingSearchService;
    private readonly IOrderQueryService _orderQueryService;
    private readonly ICompareService _compareService;
    private readonly IChatQueryService _chatQueryService;
    private readonly INotificationQueryService _notificationQueryService;
    private readonly IVerificationQueryService _verificationQueryService;
    private readonly IReviewQueryService _reviewQueryService;
    private readonly IAdminInsightsService _adminInsightsService;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardQueryService(
        IUnitOfWork unitOfWork,
        IListingQueryService listingQueryService,
        IListingSearchService listingSearchService,
        IOrderQueryService orderQueryService,
        ICompareService compareService,
        IChatQueryService chatQueryService,
        INotificationQueryService notificationQueryService,
        IVerificationQueryService verificationQueryService,
        IReviewQueryService reviewQueryService,
        IAdminInsightsService adminInsightsService,
        UserManager<ApplicationUser> userManager)
    {
        _unitOfWork = unitOfWork;
        _listingQueryService = listingQueryService;
        _listingSearchService = listingSearchService;
        _orderQueryService = orderQueryService;
        _compareService = compareService;
        _chatQueryService = chatQueryService;
        _notificationQueryService = notificationQueryService;
        _verificationQueryService = verificationQueryService;
        _reviewQueryService = reviewQueryService;
        _adminInsightsService = adminInsightsService;
        _userManager = userManager;
    }

    public async Task<StudentDashboardStatsDto> GetStudentDashboardAsync(string userId)
    {
        var now = DateTime.UtcNow;
        var thisMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var listingRepo = _unitOfWork.Repository<Listing>();
        var aiLogRepo = _unitOfWork.Repository<AIUsageLog>();
        var orderRepo = _unitOfWork.Repository<Order>();

        var verification = await _verificationQueryService.GetCurrentStatusAsync(userId);
        var isVerified = await _verificationQueryService.IsUserVerifiedAsync(userId);
        var savedListings = await _listingSearchService.GetMySavedListingsAsync(userId, 1, 1);
        var compareIds = await _compareService.GetCompareListingIdsAsync(userId);

        return new StudentDashboardStatsDto
        {
            ListingStats = await BuildMyListingStatusCountsAsync(listingRepo, orderRepo, userId),
            BuyerOrderStats = await BuildMyOrderStatusCountsAsync(userId, isBuyerSide: true),
            SellerOrderStats = await BuildMyOrderStatusCountsAsync(userId, isBuyerSide: false),
            SavedListingsCount = savedListings.TotalCount,
            CompareCount = compareIds.Count,
            UnreadMessagesCount = await _chatQueryService.GetUnreadCountAsync(userId),
            UnreadNotificationsCount = await _notificationQueryService.GetUnreadCountAsync(userId),
            IsVerified = isVerified,
            Verification = verification,
            Reputation = await _reviewQueryService.GetReputationAsync(userId),
            AIUsageThisMonthCount = await aiLogRepo.CountAsync(l => l.UserId == userId && l.CreatedAtUtc >= thisMonthStart),
            RecommendedListings = await BuildRecommendedListingsAsync(userId, verification),
            RecentActivity = await BuildStudentRecentActivityAsync(userId),
        };
    }

    public async Task<AdminDashboardStatsDto> GetAdminDashboardAsync()
    {
        var now = DateTime.UtcNow;
        var thisMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastMonthStart = thisMonthStart.AddMonths(-1);

        var listingRepo = _unitOfWork.Repository<Listing>();
        var orderRepo = _unitOfWork.Repository<Order>();
        var reviewRepo = _unitOfWork.Repository<Review>();
        var conversationRepo = _unitOfWork.Repository<Conversation>();
        var messageRepo = _unitOfWork.Repository<Message>();

        var listingStats = await _listingQueryService.GetModerationStatsAsync();
        var verificationStats = await _verificationQueryService.GetStatisticsAsync();
        var reportedReviews = await _reviewQueryService.GetAdminQueueAsync(reportedOnly: true, pageNumber: 1, pageSize: 1);
        var insights = await _adminInsightsService.GetStatsAsync();
        var orderStats = await _orderQueryService.GetStatisticsAsync();

        var totalUsers = await _userManager.Users.CountAsync();
        var totalStudents = (await _userManager.GetUsersInRoleAsync(Roles.Student)).Count;
        var totalAdmins = (await _userManager.GetUsersInRoleAsync(Roles.Admin)).Count;

        Expression<Func<Order, bool>> completedNotDonation = o => o.Status == OrderStatus.Completed && !o.IsDonation;

        return new AdminDashboardStatsDto
        {
            ListingStats = listingStats,
            VerificationStats = verificationStats,
            ReportedReviewCount = reportedReviews.TotalCount,
            Insights = insights,

            DraftListings = await listingRepo.CountAsync(l => l.Status == ListingStatus.Draft),
            ReservedListings = await listingRepo.CountAsync(l => l.Status == ListingStatus.Reserved),
            // Completed non-donation orders, not a ListingStatus.Sold count — see AdminDashboardStatsDto.SoldListings remarks.
            SoldListings = await orderRepo.CountAsync(completedNotDonation),
            OutOfStockListings = await listingRepo.CountAsync(l => l.Status == ListingStatus.OutOfStock),

            TotalUsers = totalUsers,
            TotalStudents = totalStudents,
            TotalAdmins = totalAdmins,
            ActiveUsers = await _userManager.Users.CountAsync(u => u.IsActive),
            UsersThisMonth = await _userManager.Users.CountAsync(u => u.CreatedAtUtc >= thisMonthStart),
            UsersLastMonth = await _userManager.Users.CountAsync(u => u.CreatedAtUtc >= lastMonthStart && u.CreatedAtUtc < thisMonthStart),
            RegistrationGrowth = await BuildRegistrationGrowthAsync(),

            OrderStats = orderStats,

            TotalRevenue = await orderRepo.SumAsync(completedNotDonation, o => o.Listing.Price.Amount),
            RevenueThisMonth = await orderRepo.SumAsync(
                o => o.Status == OrderStatus.Completed && !o.IsDonation && o.CompletedAtUtc >= thisMonthStart,
                o => o.Listing.Price.Amount),
            RevenueLastMonth = await orderRepo.SumAsync(
                o => o.Status == OrderStatus.Completed && !o.IsDonation && o.CompletedAtUtc >= lastMonthStart && o.CompletedAtUtc < thisMonthStart,
                o => o.Listing.Price.Amount),

            ReviewRatingDistribution = await BuildReviewRatingDistributionAsync(reviewRepo),

            TotalConversations = await conversationRepo.CountAsync(),
            ConversationsThisMonth = await conversationRepo.CountAsync(c => c.CreatedAtUtc >= thisMonthStart),
            TotalMessages = await messageRepo.CountAsync(),
            MessagesThisMonth = await messageRepo.CountAsync(m => m.CreatedAtUtc >= thisMonthStart),

            RecentActivity = await BuildAdminRecentActivityAsync(),
        };
    }

    private static async Task<MyListingStatusCountsDto> BuildMyListingStatusCountsAsync(
        IGenericRepository<Listing> listingRepo, IGenericRepository<Order> orderRepo, string userId)
    {
        var draft = await listingRepo.CountAsync(l => l.SellerId == userId && l.Status == ListingStatus.Draft);
        var pending = await listingRepo.CountAsync(l => l.SellerId == userId && l.Status == ListingStatus.PendingApproval);
        var active = await listingRepo.CountAsync(l => l.SellerId == userId && l.Status == ListingStatus.Active);
        var reserved = await listingRepo.CountAsync(l => l.SellerId == userId && l.Status == ListingStatus.Reserved);
        // Completed non-donation orders, not a ListingStatus.Sold count — see MyListingStatusCountsDto.Sold remarks.
        var sold = await orderRepo.CountAsync(o => o.SellerId == userId && o.Status == OrderStatus.Completed && !o.IsDonation);
        var donated = await listingRepo.CountAsync(l => l.SellerId == userId && l.Status == ListingStatus.Donated);
        var archived = await listingRepo.CountAsync(l => l.SellerId == userId && l.Status == ListingStatus.Archived);
        var rejected = await listingRepo.CountAsync(l => l.SellerId == userId && l.Status == ListingStatus.Rejected);
        var outOfStock = await listingRepo.CountAsync(l => l.SellerId == userId && l.Status == ListingStatus.OutOfStock);

        return new MyListingStatusCountsDto
        {
            Draft = draft,
            PendingApproval = pending,
            Active = active,
            Reserved = reserved,
            Sold = sold,
            Donated = donated,
            Archived = archived,
            Rejected = rejected,
            OutOfStock = outOfStock,
            Total = draft + pending + active + reserved + donated + archived + rejected + outOfStock,
        };
    }

    /// <summary>
    /// The <c>isBuyerSide ? o.BuyerId : o.SellerId</c> ternary is plain expression-tree
    /// syntax (not a method call) so EF Core translates it straight into SQL — a
    /// captured bool closure variable inside a conditional operator, no client-eval
    /// fallback needed.
    /// </summary>
    private async Task<MyOrderStatusCountsDto> BuildMyOrderStatusCountsAsync(string userId, bool isBuyerSide)
    {
        var orderRepo = _unitOfWork.Repository<Order>();

        var pending = await orderRepo.CountAsync(o => (isBuyerSide ? o.BuyerId : o.SellerId) == userId && o.Status == OrderStatus.Pending);
        var confirmed = await orderRepo.CountAsync(o => (isBuyerSide ? o.BuyerId : o.SellerId) == userId && o.Status == OrderStatus.Confirmed);
        var readyForPickup = await orderRepo.CountAsync(o => (isBuyerSide ? o.BuyerId : o.SellerId) == userId && o.Status == OrderStatus.ReadyForPickup);
        var completed = await orderRepo.CountAsync(o => (isBuyerSide ? o.BuyerId : o.SellerId) == userId && o.Status == OrderStatus.Completed);
        var cancelled = await orderRepo.CountAsync(o => (isBuyerSide ? o.BuyerId : o.SellerId) == userId && o.Status == OrderStatus.Cancelled);

        return new MyOrderStatusCountsDto
        {
            PendingCount = pending,
            ConfirmedCount = confirmed,
            ReadyForPickupCount = readyForPickup,
            CompletedCount = completed,
            CancelledCount = cancelled,
            TotalCount = pending + confirmed + readyForPickup + completed + cancelled,
        };
    }

    /// <summary>Active listings matching the student's university, excluding their own — falls back to the marketplace's "Recent" rail when they have no verification on file yet.</summary>
    private async Task<IReadOnlyList<ListingSummaryDto>> BuildRecommendedListingsAsync(string userId, VerificationDto? verification)
    {
        const int overFetch = 8;

        if (verification is not null)
        {
            var criteria = new ListingSearchCriteria
            {
                UniversityId = verification.UniversityId,
                Sort = ListingSortOption.Newest,
            };
            var page = await _listingSearchService.SearchAsync(criteria, overFetch);
            return page.Items.Where(l => l.SellerId != userId).Take(RecommendedListingsCap).ToList();
        }

        var recent = await _listingQueryService.GetRecentListingsAsync(overFetch);
        return recent.Where(l => l.SellerId != userId).Take(RecommendedListingsCap).ToList();
    }

    private async Task<IReadOnlyList<RecentActivityItemDto>> BuildStudentRecentActivityAsync(string userId)
    {
        var listingRepo = _unitOfWork.Repository<Listing>();
        var orderRepo = _unitOfWork.Repository<Order>();
        var reviewRepo = _unitOfWork.Repository<Review>();

        var items = new List<RecentActivityItemDto>();

        var myListings = await listingRepo.FindProjectedAsync(
            l => l.SellerId == userId,
            q => q.OrderByDescending(l => l.CreatedAtUtc),
            PerSourceActivityTake,
            l => new { l.Title, l.Slug, l.CreatedAtUtc });
        items.AddRange(myListings.Select(l => new RecentActivityItemDto(
            "Listing", $"Listed \"{l.Title}\"", l.CreatedAtUtc, "list", $"/Marketplace/Listing/{l.Slug}")));

        var myOrders = await orderRepo.FindProjectedAsync(
            o => o.BuyerId == userId || o.SellerId == userId,
            q => q.OrderByDescending(o => o.CreatedAtUtc),
            PerSourceActivityTake,
            o => new { o.ReferenceNumber, o.Status, ListingTitle = o.Listing.Title, o.CreatedAtUtc });
        items.AddRange(myOrders.Select(o => new RecentActivityItemDto(
            "Order", $"Order {o.ReferenceNumber} for \"{o.ListingTitle}\" — {o.Status.GetDisplayName()}", o.CreatedAtUtc, "package", null)));

        var myReviews = await reviewRepo.FindProjectedAsync(
            r => r.ReviewerId == userId,
            q => q.OrderByDescending(r => r.CreatedAtUtc),
            PerSourceActivityTake,
            r => new { r.Rating, r.CreatedAtUtc });
        items.AddRange(myReviews.Select(r => new RecentActivityItemDto(
            "Review", $"You left a {r.Rating}-star review", r.CreatedAtUtc, "star", null)));

        var myVerifications = await _verificationQueryService.GetHistoryAsync(userId, 1, PerSourceActivityTake);
        items.AddRange(myVerifications.Items.Select(v => new RecentActivityItemDto(
            "Verification", $"{v.StatusDisplay} — verification submitted", v.SubmittedAtUtc, "shield-check", null)));

        var myNotifications = await _notificationQueryService.GetHistoryAsync(userId, 1, PerSourceActivityTake);
        items.AddRange(myNotifications.Items.Select(n => new RecentActivityItemDto(
            "Notification", n.Title, n.CreatedAtUtc, "bell", n.Link)));

        return items.OrderByDescending(i => i.TimestampUtc).Take(StudentRecentActivityCap).ToList();
    }

    private async Task<IReadOnlyList<RecentActivityItemDto>> BuildAdminRecentActivityAsync()
    {
        var listingRepo = _unitOfWork.Repository<Listing>();
        var orderRepo = _unitOfWork.Repository<Order>();
        var reviewRepo = _unitOfWork.Repository<Review>();
        var verificationRepo = _unitOfWork.Repository<StudentVerification>();

        var items = new List<RecentActivityItemDto>();

        var newestListings = await listingRepo.FindProjectedAsync(
            _ => true,
            q => q.OrderByDescending(l => l.CreatedAtUtc),
            PerSourceActivityTake,
            l => new { l.Title, l.Slug, l.Status, l.CreatedAtUtc });
        items.AddRange(newestListings.Select(l => new RecentActivityItemDto(
            "Listing", $"\"{l.Title}\" listed — {l.Status.GetDisplayName()}", l.CreatedAtUtc, "list", $"/Marketplace/Listing/{l.Slug}")));

        var newestOrders = await orderRepo.FindProjectedAsync(
            _ => true,
            q => q.OrderByDescending(o => o.CreatedAtUtc),
            PerSourceActivityTake,
            o => new { o.ReferenceNumber, o.Status, ListingTitle = o.Listing.Title, o.CreatedAtUtc });
        items.AddRange(newestOrders.Select(o => new RecentActivityItemDto(
            "Order", $"Order {o.ReferenceNumber} for \"{o.ListingTitle}\" — {o.Status.GetDisplayName()}", o.CreatedAtUtc, "package", null)));

        var newestVerifications = await verificationRepo.FindProjectedAsync(
            _ => true,
            q => q.OrderByDescending(v => v.SubmittedAtUtc),
            PerSourceActivityTake,
            v => new { v.StudentNumber, v.Status, v.SubmittedAtUtc });
        items.AddRange(newestVerifications.Select(v => new RecentActivityItemDto(
            "Verification", $"Verification submitted by student #{v.StudentNumber} — {v.Status.GetDisplayName()}", v.SubmittedAtUtc, "shield-check", null)));

        var newestReviews = await reviewRepo.FindProjectedAsync(
            _ => true,
            q => q.OrderByDescending(r => r.CreatedAtUtc),
            PerSourceActivityTake,
            r => new { r.Rating, r.CreatedAtUtc });
        items.AddRange(newestReviews.Select(r => new RecentActivityItemDto(
            "Review", $"A {r.Rating}-star review was posted", r.CreatedAtUtc, "star", null)));

        var newestUsers = await _userManager.Users
            .OrderByDescending(u => u.CreatedAtUtc)
            .Take(PerSourceActivityTake)
            .Select(u => new { u.FirstName, u.LastName, u.CreatedAtUtc })
            .ToListAsync();
        items.AddRange(newestUsers.Select(u => new RecentActivityItemDto(
            "User", $"{u.FirstName} {u.LastName} joined", u.CreatedAtUtc, "user-plus", null)));

        return items.OrderByDescending(i => i.TimestampUtc).Take(AdminRecentActivityCap).ToList();
    }

    /// <summary>Same 5-CountAsync-per-star shape as <see cref="AI.AdminInsightsService"/>'s platform average rating helper, kept as a distribution instead of collapsed to one number.</summary>
    private static async Task<IReadOnlyList<int>> BuildReviewRatingDistributionAsync(IGenericRepository<Review> reviewRepo)
    {
        var distribution = new int[5];
        for (var star = 1; star <= 5; star++)
        {
            distribution[star - 1] = await reviewRepo.CountAsync(r => r.Rating == star);
        }

        return distribution;
    }

    private async Task<IReadOnlyList<NameCountDto>> BuildRegistrationGrowthAsync()
    {
        var now = DateTime.UtcNow;
        var currentMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var growth = new List<NameCountDto>();
        for (var i = RegistrationGrowthMonths - 1; i >= 0; i--)
        {
            var monthStart = currentMonthStart.AddMonths(-i);
            var monthEnd = monthStart.AddMonths(1);
            var count = await _userManager.Users.CountAsync(u => u.CreatedAtUtc >= monthStart && u.CreatedAtUtc < monthEnd);
            growth.Add(new NameCountDto(monthStart.ToString("MMM yyyy"), count));
        }

        return growth;
    }
}
