using SajhaSikshya.DTOs.Marketplace;

namespace SajhaSikshya.ViewModels.Marketplace;

/// <summary>Backs the public listing details page (<c>/marketplace/details/{slug}</c>).</summary>
public class ListingDetailsViewModel
{
    public ListingDto Listing { get; set; } = null!;

    public SellerProfileDto? SellerProfile { get; set; }

    public IReadOnlyList<ListingSummaryDto> RelatedListings { get; set; } = Array.Empty<ListingSummaryDto>();
}
