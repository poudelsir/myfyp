using SajhaSikshya.Data.Enums;

namespace SajhaSikshya.DTOs.Marketplace;

/// <summary>
/// One row of a seller's "My Listings Performance" view (see
/// <see cref="Services.Interfaces.Marketplace.IListingQueryService.GetMyListingPerformanceAsync"/>) —
/// how many students looked at, saved, and actually completed an order for this
/// listing. Not part of <see cref="ListingDto"/>/<see cref="ListingSummaryDto"/> since
/// nowhere else needs the saved/completed counts alongside a listing's core fields.
/// </summary>
public class ListingPerformanceDto
{
    public int ListingId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? ThumbnailImagePath { get; set; }

    public ListingStatus Status { get; set; }

    public string StatusDisplay { get; set; } = string.Empty;

    public int ViewCount { get; set; }

    public int SavedCount { get; set; }

    public int CompletedOrderCount { get; set; }

    /// <summary>CompletedOrderCount / ViewCount as a percentage, 0 when there are no views yet (never divides by zero).</summary>
    public double ConversionRatePercent { get; set; }
}
