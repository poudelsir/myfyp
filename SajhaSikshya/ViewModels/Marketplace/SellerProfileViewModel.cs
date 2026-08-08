using SajhaSikshya.DTOs;
using SajhaSikshya.DTOs.Marketplace;

namespace SajhaSikshya.ViewModels.Marketplace;

/// <summary>Backs a seller's public storefront page (<c>/marketplace/seller/{sellerId}</c>).</summary>
public class SellerProfileViewModel
{
    public SellerProfileDto Seller { get; set; } = null!;

    public PagedResult<ListingSummaryDto> Listings { get; set; } = new();
}
