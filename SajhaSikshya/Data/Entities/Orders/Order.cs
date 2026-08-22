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
    /// Set once at creation from the buyer's choice — <see cref="Enums.PaymentMethod.CashOnPickup"/>
    /// by default, <see cref="Enums.PaymentMethod.Unknown"/> for a donation, or
    /// <see cref="Enums.PaymentMethod.ESewa"/>/<see cref="Enums.PaymentMethod.Khalti"/> if the
    /// buyer opts into the simulated online-payment flow. Never changed after creation.
    /// </summary>
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Unknown;

    /// <summary>Independent of <see cref="Status"/> — see <see cref="Enums.PaymentStatus"/>'s remarks for why payment and pickup progress are tracked separately.</summary>
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.NotApplicable;

    /// <summary>Simulated gateway's fake reference (e.g. "SIM-ESEWA-A1B2C3D4"), set only once <see cref="PaymentStatus"/> reaches <see cref="Enums.PaymentStatus.Completed"/>.</summary>
    [StringLength(64)]
    public string? PaymentTransactionId { get; set; }

    public DateTime? PaymentCompletedAtUtc { get; set; }

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
