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

    /// <summary>Completed, non-donation orders for this seller — NOT a count of <see cref="Data.Enums.ListingStatus.Sold"/> rows. Since Phase 11.5 (inventory/stock), a completed sale moves a listing to Active (stock remains) or OutOfStock (stock hits 0), never Sold, so a listing can be sold many times over its life; "how many times has this seller sold something" is a completed-order count, not a listing-status count.</summary>
    public int Sold { get; set; }

    public int Donated { get; set; }

    public int Archived { get; set; }

    public int Rejected { get; set; }

    public int OutOfStock { get; set; }

    public int Total { get; set; }
}
