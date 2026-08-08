using System.ComponentModel.DataAnnotations;

namespace SajhaSikshya.Data.Enums;

/// <summary>
/// Lifecycle state of a marketplace order, from the moment a buyer places it
/// through to pickup/completion or cancellation.
/// </summary>
public enum OrderStatus
{
    /// <summary>Placed by the buyer, awaiting seller confirmation.</summary>
    [Display(Name = "Pending", Description = "Placed by the buyer, awaiting seller confirmation.")]
    Pending = 0,

    /// <summary>Confirmed by the seller and being prepared.</summary>
    [Display(Name = "Confirmed", Description = "Confirmed by the seller and being prepared.")]
    Confirmed = 1,

    /// <summary>Prepared and ready for the buyer to collect.</summary>
    [Display(Name = "Ready for Pickup", Description = "Prepared and ready for the buyer to collect.")]
    ReadyForPickup = 2,

    /// <summary>Picked up by the buyer; the transaction is complete.</summary>
    [Display(Name = "Completed", Description = "Picked up and the transaction is complete.")]
    Completed = 3,

    /// <summary>Cancelled by the buyer, seller, or an admin before completion.</summary>
    [Display(Name = "Cancelled", Description = "Cancelled by the buyer, seller, or an admin.")]
    Cancelled = 4,
}
