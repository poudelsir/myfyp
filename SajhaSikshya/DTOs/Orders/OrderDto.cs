using SajhaSikshya.Data.Enums;

namespace SajhaSikshya.DTOs.Orders;

/// <summary>List-shaped projection of an <see cref="Data.Entities.Orders.Order"/> — "My Orders" (buying/selling) and the Admin orders list all use this shape; <see cref="OrderDetailDto"/> is the richer single-order view.</summary>
public class OrderDto
{
    public int Id { get; set; }

    /// <summary>Human-facing, permanent reference number (e.g. "ORD-2026-000047") — stored on the entity, generated once at creation.</summary>
    public string ReferenceNumber { get; set; } = string.Empty;

    public int ListingId { get; set; }

    public string ListingTitle { get; set; } = string.Empty;

    public string ListingSlug { get; set; } = string.Empty;

    public string? ListingThumbnailImagePath { get; set; }

    public string BuyerId { get; set; } = string.Empty;

    public string BuyerName { get; set; } = string.Empty;

    public string? BuyerProfilePicturePath { get; set; }

    public string SellerId { get; set; } = string.Empty;

    public string SellerName { get; set; } = string.Empty;

    public string? SellerProfilePicturePath { get; set; }

    public OrderStatus Status { get; set; }

    public string StatusDisplay { get; set; } = string.Empty;

    public bool IsDonation { get; set; }

    public decimal PriceAmount { get; set; }

    public string FormattedPrice { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ConfirmedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public DateTime? CancelledAtUtc { get; set; }
}
