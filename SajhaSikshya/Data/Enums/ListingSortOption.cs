using System.ComponentModel.DataAnnotations;

namespace SajhaSikshya.Data.Enums;

/// <summary>How the marketplace browse/search grid orders its results. Ignored (relevance wins instead) whenever a keyword search is active — see <see cref="Services.Marketplace.ListingSearchService"/>.</summary>
public enum ListingSortOption
{
    [Display(Name = "Newest First")]
    Newest = 0,

    [Display(Name = "Oldest First")]
    Oldest = 1,

    [Display(Name = "Price: Low to High")]
    PriceLowToHigh = 2,

    [Display(Name = "Price: High to Low")]
    PriceHighToLow = 3,

    [Display(Name = "Most Viewed")]
    MostViewed = 4,

    [Display(Name = "Alphabetical (A-Z)")]
    Alphabetical = 5,

    [Display(Name = "Donations First")]
    DonationFirst = 6,
}
