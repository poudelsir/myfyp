using System.ComponentModel.DataAnnotations;
using SajhaSikshya.Constants;
using SajhaSikshya.Data.Entities.Marketplace;
using SajhaSikshya.Data.Enums;

namespace SajhaSikshya.Data.Entities.Orders;

/// <summary>
/// A single purchase or donation request against a <see cref="Marketplace.Listing"/>.
/// Never overwritten across its lifecycle — every field on this row reflects the
/// *current* state; the full trail of how it got there lives in
/// <see cref="OrderStatusHistory"/> (see <see cref="Services.Orders.OrderService"/>'s
/// remarks on why history is a separate append-only table rather than derived from
/// this row alone).
/// </summary>
public class Order : BaseEntity
{
    /// <summary>
    /// Human-facing, permanent identifier (e.g. "ORD-2026-000047") — set exactly once
    /// by <see cref="Services.Orders.OrderService.CreateOrderAsync"/> right after the
    /// row's identity <see cref="BaseEntity.Id"/> is generated, and never touched again.
    /// Stored (not computed from Id at read time) precisely so a future formatting
    /// change can never retroactively alter a reference already printed on a receipt
    /// or handed to a payment gateway. The numeric segment is the raw Id — unique and
    /// race-free for free via the identity column — rather than a per-year-resetting
    /// counter, which would need its own stateful sequence for no real benefit here.
    /// </summary>
    [Required]
    [StringLength(32)]
    public string ReferenceNumber { get; set; } = string.Empty;

    /// <summary>
    /// Schema preparation for a future payment module — set once at creation
    /// (<see cref="Enums.PaymentMethod.CashOnPickup"/> for a sale,
    /// <see cref="Enums.PaymentMethod.Unknown"/> for a donation) and not otherwise
    /// read or written anywhere yet. No processing/gateway logic exists in this phase.
    /// </summary>
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Unknown;

    [Required]
    public string BuyerId { get; set; } = string.Empty;

    public ApplicationUser Buyer { get; set; } = null!;

    [Required]
    public string SellerId { get; set; } = string.Empty;

    public ApplicationUser Seller { get; set; } = null!;

    public int ListingId { get; set; }

    public Listing Listing { get; set; } = null!;

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    /// <summary>Mirrors <see cref="Marketplace.Listing.IsDonation"/> at the moment this order was created — denormalized so the order's own donation status can't drift if the listing were ever edited later.</summary>
    public bool IsDonation { get; set; }

    [StringLength(OrderConstants.MaximumPickupNotesLength)]
    public string? PickupNotes { get; set; }

    [StringLength(OrderConstants.MaximumReasonLength)]
    public string? CancellationReason { get; set; }

    public DateTime? ConfirmedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public DateTime? CancelledAtUtc { get; set; }

    /// <summary>
    /// Who initiated this order row — in every path this phase actually builds, that's
    /// always the buyer (only buyers create orders), so this deliberately isn't a
    /// separate FK/navigation the way <c>ReviewedByUserId</c> is on
    /// <c>StudentVerification</c> (which the UI displays distinctly from the record's
    /// owner). Kept as a plain audit column rather than duplicated FK ceremony to the
    /// same Users table a third time (Buyer, Seller, and this) for a value nothing
    /// currently renders separately from <see cref="BuyerId"/>.
    /// </summary>
    [Required]
    public string CreatedByUserId { get; set; } = string.Empty;

    public ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();
}
