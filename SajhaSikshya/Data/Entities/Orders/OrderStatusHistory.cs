using System.ComponentModel.DataAnnotations;
using SajhaSikshya.Constants;
using SajhaSikshya.Data.Enums;

namespace SajhaSikshya.Data.Entities.Orders;

/// <summary>
/// One immutable record of an <see cref="Order"/> moving from one <see cref="OrderStatus"/>
/// to another. Append-only — nothing in this codebase ever updates or removes a row
/// here, only inserts a new one per transition, giving Order a complete audit trail
/// the same way <c>StudentVerification</c>'s history is a full row per submission
/// rather than a single mutable "last state".
/// </summary>
public class OrderStatusHistory : BaseEntity
{
    public int OrderId { get; set; }

    public Order Order { get; set; } = null!;

    /// <summary>Null for the very first row (order creation) — there is no prior status to record, the order simply came into existence at <see cref="OrderStatus.Pending"/>.</summary>
    public OrderStatus? OldStatus { get; set; }

    public OrderStatus NewStatus { get; set; }

    [Required]
    public string ChangedByUserId { get; set; } = string.Empty;

    public ApplicationUser? ChangedByUser { get; set; }

    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;

    [StringLength(OrderConstants.MaximumReasonLength)]
    public string? Reason { get; set; }
}
