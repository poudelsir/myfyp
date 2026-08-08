using SajhaSikshya.Data.Enums;
using SajhaSikshya.DTOs;
using SajhaSikshya.DTOs.Orders;

namespace SajhaSikshya.ViewModels.Orders;

/// <summary>Backs the Student "My Orders" page — one view, toggled between the Buying and Selling side of the same Student's orders.</summary>
public class MyOrdersViewModel
{
    /// <summary>"buying" or "selling".</summary>
    public string Tab { get; set; } = "buying";

    public OrderStatus? Status { get; set; }

    public PagedResult<OrderDto> Page { get; set; } = new();
}
