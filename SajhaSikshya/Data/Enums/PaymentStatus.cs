using System.ComponentModel.DataAnnotations;

namespace SajhaSikshya.Data.Enums;

/// <summary>
/// Where an order's payment stands, independent of <see cref="OrderStatus"/> — a buyer
/// can pre-pay through the simulated gateway (see <see cref="Services.Payments.IPaymentGatewayService"/>)
/// long before the seller confirms pickup, so this can't just be folded into the pickup
/// lifecycle. Set once at order creation from the buyer's chosen <see cref="PaymentMethod"/>
/// and updated only by the simulated gateway callback thereafter.
/// </summary>
public enum PaymentStatus
{
    /// <summary>Cash on pickup, or a donation — no online payment is ever expected for this order.</summary>
    [Display(Name = "Not Applicable", Description = "No online payment required for this order.")]
    NotApplicable = 0,

    /// <summary>Buyer chose an online method but hasn't completed (or has abandoned) the simulated checkout yet.</summary>
    [Display(Name = "Awaiting Payment", Description = "Waiting for the buyer to complete payment.")]
    Pending = 1,

    /// <summary>Simulated gateway reported success.</summary>
    [Display(Name = "Paid", Description = "Payment completed.")]
    Completed = 2,

    /// <summary>Simulated gateway reported failure; the buyer may retry or switch to Cash on Pickup.</summary>
    [Display(Name = "Payment Failed", Description = "The payment attempt failed.")]
    Failed = 3,
}
