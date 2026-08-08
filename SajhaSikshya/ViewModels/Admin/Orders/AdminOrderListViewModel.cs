using SajhaSikshya.Data.Enums;
using SajhaSikshya.DTOs;
using SajhaSikshya.DTOs.Orders;

namespace SajhaSikshya.ViewModels.Admin.Orders;

/// <summary>Backs the Admin Orders dashboard — the paged, filterable order list plus the aggregate stat cards shown above it. Mirrors <c>AdminVerificationListViewModel</c>'s shape.</summary>
public class AdminOrderListViewModel
{
    public PagedResult<OrderDto> Page { get; set; } = new();

    public OrderStatisticsDto Statistics { get; set; } = new();

    public string? SearchTerm { get; set; }

    public OrderStatus? Status { get; set; }
}
