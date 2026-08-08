using System.ComponentModel.DataAnnotations;

namespace SajhaSikshya.Data.Enums;

/// <summary>Which direction a <see cref="Entities.Reviews.Review"/> was written in — a marketplace transaction has two independent reputations, not one.</summary>
public enum ReviewType
{
    /// <summary>Written by the buyer, about the seller.</summary>
    [Display(Name = "Buyer Review", Description = "A review written by the buyer, about the seller.")]
    BuyerToSeller = 0,

    /// <summary>Written by the seller, about the buyer.</summary>
    [Display(Name = "Seller Review", Description = "A review written by the seller, about the buyer.")]
    SellerToBuyer = 1,
}
