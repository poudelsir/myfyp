namespace SajhaSikshya.DTOs.Dashboard;

/// <summary>
/// A Student's own listings broken down by every <see cref="Data.Enums.ListingStatus"/>
/// value. Deliberately not a reuse of <see cref="Marketplace.ListingModerationStatsDto"/> —
/// that DTO is admin-shaped (Pending/Approved/Rejected/Archived/Donation) and omits
/// Draft/Reserved/Sold, which a seller's own dashboard needs to show.
/// </summary>
public class MyListingStatusCountsDto
{
    public int Draft { get; set; }

    public int PendingApproval { get; set; }

    public int Active { get; set; }

    public int Reserved { get; set; }

    public int Sold { get; set; }

    public int Donated { get; set; }

    public int Archived { get; set; }

    public int Rejected { get; set; }

    public int Total { get; set; }
}
