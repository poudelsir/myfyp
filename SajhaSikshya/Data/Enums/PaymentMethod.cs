using System.ComponentModel.DataAnnotations;

namespace SajhaSikshya.Data.Enums;

/// <summary>
/// How an <see cref="Entities.Orders.Order"/>'s exchange was (or will be) settled.
/// Pure schema preparation for a future payment module — nothing in this codebase
/// processes a payment or talks to a gateway yet. <see cref="Services.Orders.OrderService"/>
/// sets this once at order creation (<see cref="CashOnPickup"/> for a real sale,
/// <see cref="Unknown"/> for a donation, since no payment changes hands) and never
/// touches it again.
/// </summary>
public enum PaymentMethod
{
    [Display(Name = "Unknown", Description = "No payment method recorded — used for donations, where none applies.")]
    Unknown = 0,

    [Display(Name = "Cash on Pickup", Description = "Paid in person at pickup.")]
    CashOnPickup = 1,

    [Display(Name = "eSewa", Description = "Paid via eSewa.")]
    ESewa = 2,

    [Display(Name = "Khalti", Description = "Paid via Khalti.")]
    Khalti = 3,
}
