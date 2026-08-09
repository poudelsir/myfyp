using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SajhaSikshya.Constants;
using SajhaSikshya.Data.Constants;
using SajhaSikshya.Data.Entities;
using SajhaSikshya.DTOs;
using SajhaSikshya.DTOs.Users;
using SajhaSikshya.Services.Interfaces.Marketplace;
using SajhaSikshya.Services.Interfaces.Notifications;
using SajhaSikshya.Services.Interfaces.Orders;
using SajhaSikshya.Services.Interfaces.Reviews;
using SajhaSikshya.Services.Interfaces.Users;
using SajhaSikshya.Services.Interfaces.Verification;

namespace SajhaSikshya.Services.Users;

public class UserQueryService : IUserQueryService
{
    private const int RecentNotificationsTake = 5;

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IVerificationQueryService _verificationQueryService;
    private readonly IListingQueryService _listingQueryService;
    private readonly IOrderQueryService _orderQueryService;
    private readonly IReviewQueryService _reviewQueryService;
    private readonly INotificationQueryService _notificationQueryService;

    public UserQueryService(
        UserManager<ApplicationUser> userManager,
        IVerificationQueryService verificationQueryService,
        IListingQueryService listingQueryService,
        IOrderQueryService orderQueryService,
        IReviewQueryService reviewQueryService,
        INotificationQueryService notificationQueryService)
    {
        _userManager = userManager;
        _verificationQueryService = verificationQueryService;
        _listingQueryService = listingQueryService;
        _orderQueryService = orderQueryService;
        _reviewQueryService = reviewQueryService;
        _notificationQueryService = notificationQueryService;
    }

    public async Task<PagedResult<AdminUserListItemDto>> GetUsersAsync(string? searchTerm, string? role, bool? isActive, int pageNumber, int pageSize)
    {
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(u =>
                u.FirstName.Contains(searchTerm) ||
                u.LastName.Contains(searchTerm) ||
                (u.Email != null && u.Email.Contains(searchTerm)));
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        // UserManager.Users alone carries no role membership — GetUsersInRoleAsync is the
        // only way this app's Identity setup exposes it, so a role filter resolves to an
        // id set first (roles are few and small: just Admin/Student) rather than a join.
        if (!string.IsNullOrWhiteSpace(role))
        {
            var idsInRole = (await _userManager.GetUsersInRoleAsync(role)).Select(u => u.Id).ToHashSet();
            query = query.Where(u => idsInRole.Contains(u.Id));
        }

        var totalCount = await query.CountAsync();
        var users = await query
            .OrderByDescending(u => u.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var userIds = users.Select(u => u.Id).ToList();
        var verifiedIds = await _verificationQueryService.GetVerifiedUserIdsAsync(userIds);

        var items = new List<AdminUserListItemDto>();
        foreach (var user in users)
        {
            var isAdmin = await _userManager.IsInRoleAsync(user, Roles.Admin);
            var currentVerification = await _verificationQueryService.GetCurrentStatusAsync(user.Id);

            items.Add(new AdminUserListItemDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                ProfilePicturePath = user.ProfilePicturePath,
                Role = isAdmin ? Roles.Admin : Roles.Student,
                IsVerifiedSeller = verifiedIds.Contains(user.Id),
                VerificationStatus = currentVerification?.Status,
                VerificationStatusDisplay = currentVerification?.StatusDisplay,
                IsActive = user.IsActive,
                CreatedAtUtc = user.CreatedAtUtc,
            });
        }

        return new PagedResult<AdminUserListItemDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<AdminUserDetailDto?> GetUserDetailAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return null;
        }

        var isAdmin = await _userManager.IsInRoleAsync(user, Roles.Admin);
        var currentVerification = await _verificationQueryService.GetCurrentStatusAsync(userId);
        var isVerifiedSeller = await _verificationQueryService.IsUserVerifiedAsync(userId);

        var listingCount = (await _listingQueryService.GetMyListingsAsync(userId, null, null, 1, 1)).TotalCount;
        var buyerOrderCount = (await _orderQueryService.GetBuyerOrdersAsync(userId, null, 1, 1)).TotalCount;
        var sellerOrderCount = (await _orderQueryService.GetSellerOrdersAsync(userId, null, 1, 1)).TotalCount;
        var reputation = await _reviewQueryService.GetReputationAsync(userId);
        var unreadNotifications = await _notificationQueryService.GetUnreadCountAsync(userId);
        var recentNotifications = await _notificationQueryService.GetHistoryAsync(userId, 1, RecentNotificationsTake);
        var verificationHistory = await _verificationQueryService.GetHistoryAsync(userId, 1, PaginationConstants.DefaultPageSize);

        return new AdminUserDetailDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address,
            Institution = user.Institution,
            Bio = user.Bio,
            ProfilePicturePath = user.ProfilePicturePath,
            Role = isAdmin ? Roles.Admin : Roles.Student,
            IsActive = user.IsActive,
            CreatedAtUtc = user.CreatedAtUtc,
            LastLoginAtUtc = user.LastLoginAtUtc,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            CurrentVerification = currentVerification,
            IsVerifiedSeller = isVerifiedSeller,
            ListingCount = listingCount,
            BuyerOrderCount = buyerOrderCount,
            SellerOrderCount = sellerOrderCount,
            Reputation = reputation,
            UnreadNotificationCount = unreadNotifications,
            RecentNotifications = recentNotifications.Items,
            VerificationHistory = verificationHistory,
        };
    }
}
