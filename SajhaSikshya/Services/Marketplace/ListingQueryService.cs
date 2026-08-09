using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SajhaSikshya.Data.Entities;
using SajhaSikshya.Data.Entities.Marketplace;
using SajhaSikshya.Data.Enums;
using SajhaSikshya.Data.ValueObjects;
using SajhaSikshya.DTOs;
using SajhaSikshya.DTOs.Marketplace;
using SajhaSikshya.Extensions;
using SajhaSikshya.Mappings.Marketplace;
using SajhaSikshya.Repositories.Interfaces;
using SajhaSikshya.Services.Interfaces.Catalog;
using SajhaSikshya.Services.Interfaces.Marketplace;

namespace SajhaSikshya.Services.Marketplace;

public class ListingQueryService : IListingQueryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICategoryService _categoryService;

    public ListingQueryService(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager, ICategoryService categoryService)
    {
        _unitOfWork = unitOfWork;
        _userManager = userManager;
        _categoryService = categoryService;
    }

    public async Task<PagedResult<ListingDto>> GetMyListingsAsync(string sellerId, string? searchTerm, ListingStatus? status, int? categoryId, int pageNumber, int pageSize)
    {
        var repository = _unitOfWork.Repository<Listing>();

        // Precompute outside the expression tree — same reasoning as GetAllForAdminAsync.
        var hasSearchTerm = !string.IsNullOrWhiteSpace(searchTerm);
        var matchingCategoryIds = categoryId.HasValue
            ? await _categoryService.GetCategoryAndDescendantIdsAsync(categoryId.Value)
            : null;

        Expression<Func<Listing, bool>> filter = l =>
            l.SellerId == sellerId
            && (!status.HasValue || l.Status == status.Value)
            && (matchingCategoryIds == null || matchingCategoryIds.Contains(l.CategoryId))
            && (!hasSearchTerm || l.Title.Contains(searchTerm!));

        var page = await repository.GetPagedAsync(
            pageNumber,
            pageSize,
            filter: filter,
            orderBy: q => q.OrderByDescending(l => l.CreatedAtUtc),
            include: IncludeListingDetails);

        return new PagedResult<ListingDto>
        {
            Items = page.Items.Select(l => l.ToDto()).ToList(),
            PageNumber = page.PageNumber,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
        };
    }

    public async Task<ListingDto?> GetForSellerAsync(string sellerId, int id)
    {
        var repository = _unitOfWork.Repository<Listing>();
        var listing = await repository.FirstOrDefaultAsync(l => l.Id == id && l.SellerId == sellerId, IncludeListingDetails);
        return listing?.ToDto();
    }

    public async Task<IReadOnlyList<ListingSummaryDto>> GetFeaturedListingsAsync(int count)
    {
        var repository = _unitOfWork.Repository<Listing>();
        var items = await repository.FindProjectedAsync(
            filter: l => l.Status == ListingStatus.Active,
            orderBy: q => q.OrderByDescending(l => l.ViewCount),
            take: count,
            selector: ListingMappings.ToSummaryProjection);

        ListingMappings.ApplyDisplayFields(items);
        return items;
    }

    public async Task<IReadOnlyList<ListingSummaryDto>> GetRecentListingsAsync(int count)
    {
        var repository = _unitOfWork.Repository<Listing>();
        var items = await repository.FindProjectedAsync(
            filter: l => l.Status == ListingStatus.Active,
            orderBy: q => q.OrderByDescending(l => l.CreatedAtUtc),
            take: count,
            selector: ListingMappings.ToSummaryProjection);

        ListingMappings.ApplyDisplayFields(items);
        return items;
    }

    public async Task<ListingDto?> GetPublicDetailsBySlugAsync(string slug)
    {
        var repository = _unitOfWork.Repository<Listing>();
        var listing = await repository.FirstOrDefaultAsync(
            l => l.Slug == slug && l.Status == ListingStatus.Active,
            IncludeListingDetails);

        return listing?.ToDto();
    }

    public async Task<IReadOnlyList<ListingSummaryDto>> GetRelatedListingsAsync(int listingId, int count)
    {
        var repository = _unitOfWork.Repository<Listing>();
        var listing = await repository.GetByIdAsync(listingId);
        if (listing is null)
        {
            return Array.Empty<ListingSummaryDto>();
        }

        var items = await repository.FindProjectedAsync(
            filter: l => l.Status == ListingStatus.Active && l.SubjectId == listing.SubjectId && l.Id != listingId,
            orderBy: q => q.OrderByDescending(l => l.CreatedAtUtc),
            take: count,
            selector: ListingMappings.ToSummaryProjection);

        ListingMappings.ApplyDisplayFields(items);
        return items;
    }

    public async Task<PagedResult<ListingSummaryDto>> GetPublicSellerListingsAsync(string sellerId, int pageNumber, int pageSize)
    {
        var repository = _unitOfWork.Repository<Listing>();
        var page = await repository.GetPagedProjectedAsync(
            pageNumber,
            pageSize,
            filter: l => l.Status == ListingStatus.Active && l.SellerId == sellerId,
            orderBy: q => q.OrderByDescending(l => l.CreatedAtUtc),
            selector: ListingMappings.ToSummaryProjection);

        ListingMappings.ApplyDisplayFields(page.Items);
        return page;
    }

    public async Task<SellerProfileDto?> GetSellerProfileAsync(string sellerId)
    {
        var user = await _userManager.FindByIdAsync(sellerId);
        if (user is null || !user.IsActive)
        {
            return null;
        }

        var listingRepository = _unitOfWork.Repository<Listing>();
        var activeListingCount = await listingRepository
            .CountAsync(l => l.SellerId == sellerId && l.Status == ListingStatus.Active);

        // Anything that ever actually went live at some point — excludes Draft/PendingApproval
        // (never published) and Rejected (never approved), unlike ActiveListingCount which
        // only counts what's live right now.
        var totalListingCount = await listingRepository.CountAsync(l => l.SellerId == sellerId
            && (l.Status == ListingStatus.Active || l.Status == ListingStatus.Reserved
                || l.Status == ListingStatus.Sold || l.Status == ListingStatus.Donated
                || l.Status == ListingStatus.Archived || l.Status == ListingStatus.OutOfStock));

        return new SellerProfileDto
        {
            SellerId = user.Id,
            SellerName = user.FullName,
            ProfilePicturePath = user.ProfilePicturePath,
            MemberSinceUtc = user.CreatedAtUtc,
            IsPublicProfile = user.IsPublicProfile,
            ActiveListingCount = activeListingCount,
            TotalListingCount = totalListingCount,
        };
    }

    public async Task<PagedResult<ListingDto>> GetAllForAdminAsync(string? searchTerm, ListingStatus? status, int pageNumber, int pageSize)
    {
        var repository = _unitOfWork.Repository<Listing>();

        // Precompute this outside the expression tree — calling string.IsNullOrWhiteSpace
        // *inside* an Expression<Func<Listing,bool>> risks EF Core translation issues;
        // capturing a plain bool is a safe, well-established pattern for this.
        var hasSearchTerm = !string.IsNullOrWhiteSpace(searchTerm);

        Expression<Func<Listing, bool>> filter = l =>
            (!status.HasValue || l.Status == status.Value)
            && (!hasSearchTerm || l.Title.Contains(searchTerm!));

        var page = await repository.GetPagedAsync(
            pageNumber,
            pageSize,
            filter: filter,
            orderBy: q => q.OrderByDescending(l => l.CreatedAtUtc),
            include: IncludeListingDetails);

        return new PagedResult<ListingDto>
        {
            Items = page.Items.Select(l => l.ToDto()).ToList(),
            PageNumber = page.PageNumber,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
        };
    }

    public async Task<ListingDto?> GetForAdminAsync(int listingId)
    {
        var repository = _unitOfWork.Repository<Listing>();
        var listing = await repository.FirstOrDefaultAsync(l => l.Id == listingId, IncludeListingDetails);
        return listing?.ToDto();
    }

    public async Task<ListingModerationStatsDto> GetModerationStatsAsync()
    {
        var repository = _unitOfWork.Repository<Listing>();

        return new ListingModerationStatsDto
        {
            TotalListings = await repository.CountAsync(),
            PendingCount = await repository.CountAsync(l => l.Status == ListingStatus.PendingApproval),
            ApprovedCount = await repository.CountAsync(l => l.Status == ListingStatus.Active),
            RejectedCount = await repository.CountAsync(l => l.Status == ListingStatus.Rejected),
            ArchivedCount = await repository.CountAsync(l => l.Status == ListingStatus.Archived),
            DonationCount = await repository.CountAsync(l => l.IsDonation),
        };
    }

    private static IQueryable<Listing> IncludeListingDetails(IQueryable<Listing> query) => query
        .Include(l => l.Seller)
        .Include(l => l.Category)
        .Include(l => l.Subject)
        .Include(l => l.AcademicLevel)
        .Include(l => l.University)
        .Include(l => l.ThumbnailImage)
        .Include(l => l.Images)
        .Include(l => l.LastModeratedBy);
}
