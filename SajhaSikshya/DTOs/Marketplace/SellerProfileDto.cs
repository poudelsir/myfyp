namespace SajhaSikshya.DTOs.Marketplace;

/// <summary>Public-facing seller summary shown on a listing's detail page and the seller's own listings page.</summary>
public class SellerProfileDto
{
    public string SellerId { get; set; } = string.Empty;

    public string SellerName { get; set; } = string.Empty;

    public DateTime MemberSinceUtc { get; set; }

    public int ActiveListingCount { get; set; }

    /// <summary>Whether this seller currently has an approved Student Verification — see <see cref="ListingSummaryDto.IsSellerVerified"/> for the same stamped-by-the-controller convention.</summary>
    public bool IsVerified { get; set; }

    /// <summary>Reputation as a seller (BuyerToSeller reviews), stamped on by the controller the same way <see cref="IsVerified"/> is — see <c>MarketplaceController.Seller</c>.</summary>
    public double AverageRating { get; set; }

    public int ReviewCount { get; set; }
}
