using SajhaSikshya.Data.Enums;

namespace SajhaSikshya.DTOs.Orders;

/// <summary>
/// Full single-order view — backs the Order Details page, the Receipt page (same
/// data, receipt-focused layout — no separate query needed), and the Admin review
/// page. A superset of <see cref="OrderDto"/>'s fields with nothing removed, so
/// inheritance avoids duplicating the base set (same reasoning as
/// <c>VerificationDetailDto</c> extending <c>VerificationDto</c>).
/// </summary>
public class OrderDetailDto : OrderDto
{
    public string? PickupNotes { get; set; }

    public string? CancellationReason { get; set; }

    public PaymentMethod PaymentMethod { get; set; }

    public string PaymentMethodDisplay { get; set; } = string.Empty;

    public PaymentStatus PaymentStatus { get; set; }

    public string PaymentStatusDisplay { get; set; } = string.Empty;

    public string? PaymentTransactionId { get; set; }

    public DateTime? PaymentCompletedAtUtc { get; set; }

    public string ListingDescription { get; set; } = string.Empty;

    public string BuyerEmail { get; set; } = string.Empty;

    public string SellerEmail { get; set; } = string.Empty;

    /// <summary>Oldest first — the natural reading order for a timeline.</summary>
    public IReadOnlyList<OrderStatusHistoryDto> StatusHistory { get; set; } = Array.Empty<OrderStatusHistoryDto>();
}
