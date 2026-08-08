namespace SajhaSikshya.DTOs.Orders;

/// <summary>Aggregate counts for the Admin Orders Dashboard (Phase 6.2).</summary>
public class OrderStatisticsDto
{
    public int PendingCount { get; set; }

    public int ConfirmedCount { get; set; }

    public int ReadyForPickupCount { get; set; }

    public int CompletedCount { get; set; }

    public int CancelledCount { get; set; }

    public int DonationCount { get; set; }

    public int TotalOrders { get; set; }
}
