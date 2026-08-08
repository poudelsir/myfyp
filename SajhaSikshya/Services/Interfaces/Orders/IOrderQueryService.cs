using SajhaSikshya.Data.Enums;
using SajhaSikshya.DTOs;
using SajhaSikshya.DTOs.Orders;

namespace SajhaSikshya.Services.Interfaces.Orders;

/// <summary>
/// Read-only Order queries, split out from <see cref="IOrderService"/> the same way
/// <c>IListingQueryService</c>/<c>IVerificationQueryService</c> are split from their
/// respective command services. Every method returns DTOs, never tracked entities.
/// Buyer-scoped, seller-scoped, and admin-unrestricted reads all sit side by side
/// here — same convention as the other query services, where the distinction is
/// entirely in what each method's own filter enforces. <see cref="GetOrderDetailsAsync"/>
/// is deliberately unrestricted by owner (like <c>GetForAdminAsync</c> on the Listing
/// module) — the "Buyer, Seller, or Admin only" access rule is enforced by the
/// controller after fetching, the same pattern <c>VerificationImagesController</c>
/// already established for verification documents.
/// </summary>
public interface IOrderQueryService
{
    /// <summary>A buyer's own orders, newest first, optionally narrowed by status.</summary>
    Task<PagedResult<OrderDto>> GetBuyerOrdersAsync(string buyerId, OrderStatus? status, int pageNumber, int pageSize);

    /// <summary>A seller's own orders (requests against their listings), newest first, optionally narrowed by status.</summary>
    Task<PagedResult<OrderDto>> GetSellerOrdersAsync(string sellerId, OrderStatus? status, int pageNumber, int pageSize);

    /// <summary>Full detail lookup — the Order Details page, the Receipt page, and the Admin review page all use this. Includes the full status timeline. Null if the id doesn't exist.</summary>
    Task<OrderDetailDto?> GetOrderDetailsAsync(int orderId);

    /// <summary>The Admin orders list: every order regardless of buyer/seller, optionally narrowed by status and/or a search term matched against the listing title or either party's name.</summary>
    Task<PagedResult<OrderDto>> GetAdminOrdersAsync(string? searchTerm, OrderStatus? status, int pageNumber, int pageSize);

    /// <summary>Aggregate counts for the Admin Orders Dashboard.</summary>
    Task<OrderStatisticsDto> GetStatisticsAsync();
}
