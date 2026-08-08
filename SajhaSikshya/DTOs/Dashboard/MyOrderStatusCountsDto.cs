namespace SajhaSikshya.DTOs.Dashboard;

/// <summary>
/// One side (buyer or seller) of a Student's own order status breakdown — reused for
/// both <see cref="StudentDashboardStatsDto.BuyerOrderStats"/> and
/// <see cref="StudentDashboardStatsDto.SellerOrderStats"/>, since both are the same
/// shape and only differ by which side of <see cref="Data.Entities.Orders.Order"/> is
/// filtered on.
/// </summary>
public class MyOrderStatusCountsDto
{
    public int PendingCount { get; set; }

    public int ConfirmedCount { get; set; }

    public int ReadyForPickupCount { get; set; }

    public int CompletedCount { get; set; }

    public int CancelledCount { get; set; }

    public int TotalCount { get; set; }
}
