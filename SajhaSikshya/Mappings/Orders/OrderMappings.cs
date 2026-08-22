using SajhaSikshya.Data.Entities.Orders;
using SajhaSikshya.Data.ValueObjects;
using SajhaSikshya.DTOs.Orders;
using SajhaSikshya.Extensions;

namespace SajhaSikshya.Mappings.Orders;

public static class OrderMappings
{
    /// <summary>Callers must Include Buyer/Seller/Listing first — this only reads off already-loaded navigation properties, it doesn't query.</summary>
    public static OrderDto ToDto(this Order order)
    {
        var dto = new OrderDto();
        PopulateBase(dto, order);
        return dto;
    }

    /// <summary>Richer variant of <see cref="ToDto"/> — same base fields (via the shared <see cref="PopulateBase"/> helper) plus pickup/cancellation notes, the listing description, both parties' emails, and the full status timeline. Callers must also Include Listing and StatusHistory.ChangedByUser.</summary>
    public static OrderDetailDto ToDetailDto(this Order order)
    {
        var dto = new OrderDetailDto
        {
            PickupNotes = order.PickupNotes,
            CancellationReason = order.CancellationReason,
            ListingDescription = order.Listing?.Description ?? string.Empty,
            BuyerEmail = order.Buyer?.Email ?? string.Empty,
            SellerEmail = order.Seller?.Email ?? string.Empty,
            PaymentMethod = order.PaymentMethod,
            PaymentMethodDisplay = order.PaymentMethod.GetDisplayName(),
            PaymentStatus = order.PaymentStatus,
            PaymentStatusDisplay = order.PaymentStatus.GetDisplayName(),
            PaymentTransactionId = order.PaymentTransactionId,
            PaymentCompletedAtUtc = order.PaymentCompletedAtUtc,
            StatusHistory = order.StatusHistory
                .OrderBy(h => h.ChangedAtUtc)
                .Select(h => h.ToDto())
                .ToList(),
        };
        PopulateBase(dto, order);
        return dto;
    }

    public static OrderStatusHistoryDto ToDto(this OrderStatusHistory history)
    {
        return new OrderStatusHistoryDto
        {
            OldStatusDisplay = history.OldStatus?.GetDisplayName(),
            NewStatusDisplay = history.NewStatus.GetDisplayName(),
            ChangedByName = history.ChangedByUser?.FullName ?? string.Empty,
            ChangedAtUtc = history.ChangedAtUtc,
            Reason = history.Reason,
        };
    }

    private static void PopulateBase(OrderDto dto, Order order)
    {
        dto.Id = order.Id;
        dto.ReferenceNumber = order.ReferenceNumber;
        dto.ListingId = order.ListingId;
        dto.ListingTitle = order.Listing?.Title ?? string.Empty;
        dto.ListingSlug = order.Listing?.Slug ?? string.Empty;
        dto.ListingThumbnailImagePath = order.Listing?.ThumbnailImage?.ImagePath;
        dto.BuyerId = order.BuyerId;
        dto.BuyerName = order.Buyer?.FullName ?? string.Empty;
        dto.BuyerProfilePicturePath = order.Buyer?.ProfilePicturePath;
        dto.SellerId = order.SellerId;
        dto.SellerName = order.Seller?.FullName ?? string.Empty;
        dto.SellerProfilePicturePath = order.Seller?.ProfilePicturePath;
        dto.Status = order.Status;
        dto.StatusDisplay = order.Status.GetDisplayName();
        dto.IsDonation = order.IsDonation;
        dto.PriceAmount = order.Listing?.Price.Amount ?? 0m;
        dto.FormattedPrice = order.IsDonation ? "Free (Donation)" : new Money(dto.PriceAmount).ToString();
        dto.CreatedAtUtc = order.CreatedAtUtc;
        dto.ConfirmedAtUtc = order.ConfirmedAtUtc;
        dto.CompletedAtUtc = order.CompletedAtUtc;
        dto.CancelledAtUtc = order.CancelledAtUtc;
    }
}
