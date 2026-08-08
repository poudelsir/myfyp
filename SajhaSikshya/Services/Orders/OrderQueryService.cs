using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SajhaSikshya.Data.Entities.Orders;
using SajhaSikshya.Data.Enums;
using SajhaSikshya.DTOs;
using SajhaSikshya.DTOs.Orders;
using SajhaSikshya.Mappings.Orders;
using SajhaSikshya.Repositories.Interfaces;
using SajhaSikshya.Services.Interfaces.Orders;

namespace SajhaSikshya.Services.Orders;

public class OrderQueryService : IOrderQueryService
{
    private readonly IUnitOfWork _unitOfWork;

    public OrderQueryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<OrderDto>> GetBuyerOrdersAsync(string buyerId, OrderStatus? status, int pageNumber, int pageSize)
    {
        var repository = _unitOfWork.Repository<Order>();
        var page = await repository.GetPagedAsync(
            pageNumber,
            pageSize,
            filter: o => o.BuyerId == buyerId && (!status.HasValue || o.Status == status.Value),
            orderBy: q => q.OrderByDescending(o => o.CreatedAtUtc),
            include: IncludeOrderSummary);

        return ToPagedDto(page);
    }

    public async Task<PagedResult<OrderDto>> GetSellerOrdersAsync(string sellerId, OrderStatus? status, int pageNumber, int pageSize)
    {
        var repository = _unitOfWork.Repository<Order>();
        var page = await repository.GetPagedAsync(
            pageNumber,
            pageSize,
            filter: o => o.SellerId == sellerId && (!status.HasValue || o.Status == status.Value),
            orderBy: q => q.OrderByDescending(o => o.CreatedAtUtc),
            include: IncludeOrderSummary);

        return ToPagedDto(page);
    }

    public async Task<OrderDetailDto?> GetOrderDetailsAsync(int orderId)
    {
        var repository = _unitOfWork.Repository<Order>();
        var order = await repository.FirstOrDefaultAsync(o => o.Id == orderId, IncludeOrderDetails);
        return order?.ToDetailDto();
    }

    public async Task<PagedResult<OrderDto>> GetAdminOrdersAsync(string? searchTerm, OrderStatus? status, int pageNumber, int pageSize)
    {
        var repository = _unitOfWork.Repository<Order>();

        // Precompute outside the expression tree — same safe pattern already used by
        // ListingQueryService.GetAllForAdminAsync/VerificationQueryService.GetQueueAsync.
        var hasSearchTerm = !string.IsNullOrWhiteSpace(searchTerm);

        Expression<Func<Order, bool>> filter = o =>
            (!status.HasValue || o.Status == status.Value)
            && (!hasSearchTerm
                || o.Listing.Title.Contains(searchTerm!)
                || (o.Buyer.FirstName + " " + o.Buyer.LastName).Contains(searchTerm!)
                || (o.Seller.FirstName + " " + o.Seller.LastName).Contains(searchTerm!));

        var page = await repository.GetPagedAsync(
            pageNumber,
            pageSize,
            filter: filter,
            orderBy: q => q.OrderByDescending(o => o.CreatedAtUtc),
            include: IncludeOrderSummary);

        return ToPagedDto(page);
    }

    public async Task<OrderStatisticsDto> GetStatisticsAsync()
    {
        var repository = _unitOfWork.Repository<Order>();

        return new OrderStatisticsDto
        {
            TotalOrders = await repository.CountAsync(),
            PendingCount = await repository.CountAsync(o => o.Status == OrderStatus.Pending),
            ConfirmedCount = await repository.CountAsync(o => o.Status == OrderStatus.Confirmed),
            ReadyForPickupCount = await repository.CountAsync(o => o.Status == OrderStatus.ReadyForPickup),
            CompletedCount = await repository.CountAsync(o => o.Status == OrderStatus.Completed),
            CancelledCount = await repository.CountAsync(o => o.Status == OrderStatus.Cancelled),
            DonationCount = await repository.CountAsync(o => o.IsDonation),
        };
    }

    private static PagedResult<OrderDto> ToPagedDto(PagedResult<Order> page) => new()
    {
        Items = page.Items.Select(o => o.ToDto()).ToList(),
        PageNumber = page.PageNumber,
        PageSize = page.PageSize,
        TotalCount = page.TotalCount,
    };

    /// <summary>Enough to render an order row/card (list views) — no status history.</summary>
    private static IQueryable<Order> IncludeOrderSummary(IQueryable<Order> query) => query
        .Include(o => o.Buyer)
        .Include(o => o.Seller)
        .Include(o => o.Listing)
        .ThenInclude(l => l.ThumbnailImage);

    /// <summary>Everything <see cref="IncludeOrderSummary"/> loads, plus the full status timeline for the Details/Receipt/Admin-review pages.</summary>
    private static IQueryable<Order> IncludeOrderDetails(IQueryable<Order> query) => IncludeOrderSummary(query)
        .Include(o => o.StatusHistory)
        .ThenInclude(h => h.ChangedByUser);
}
