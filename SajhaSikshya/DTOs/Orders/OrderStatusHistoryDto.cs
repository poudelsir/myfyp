namespace SajhaSikshya.DTOs.Orders;

/// <summary>One row of an order's timeline — the "Timeline"/"Order History" UI reads this directly off <see cref="OrderDetailDto.StatusHistory"/>.</summary>
public class OrderStatusHistoryDto
{
    public string? OldStatusDisplay { get; set; }

    public string NewStatusDisplay { get; set; } = string.Empty;

    public string ChangedByName { get; set; } = string.Empty;

    public DateTime ChangedAtUtc { get; set; }

    public string? Reason { get; set; }
}
