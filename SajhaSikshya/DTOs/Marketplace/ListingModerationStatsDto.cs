namespace SajhaSikshya.DTOs.Marketplace;

/// <summary>Aggregate listing counts by status, for the Admin Dashboard.</summary>
public class ListingModerationStatsDto
{
    public int TotalListings { get; set; }

    public int PendingCount { get; set; }

    public int ApprovedCount { get; set; }

    public int RejectedCount { get; set; }

    public int ArchivedCount { get; set; }

    public int DonationCount { get; set; }
}
