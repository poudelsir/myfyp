namespace SajhaSikshya.DTOs.Marketplace;

/// <summary>Public-facing seller summary shown on a listing's detail page and the seller's own listings page.</summary>
public class SellerProfileDto
{
    public string SellerId { get; set; } = string.Empty;

    public string SellerName { get; set; } = string.Empty;

    /// <summary>Public marketplace avatar — synced from the seller's approved verification Profile Photo on approval (see <c>VerificationService.SyncApprovedProfilePhotoAsync</c>). Never the private verification document itself.</summary>
    public string? ProfilePicturePath { get; set; }

    public DateTime MemberSinceUtc { get; set; }

    public int ActiveListingCount { get; set; }

    /// <summary>All-time listings that ever went live (Active/Reserved/Sold/Donated/Archived) — unlike <see cref="ActiveListingCount"/>, not limited to currently-active ones.</summary>
    public int TotalListingCount { get; set; }

    public int CompletedOrderCount { get; set; }

    /// <summary>Whether this seller currently has an approved Student Verification — see <see cref="ListingSummaryDto.IsSellerVerified"/> for the same stamped-by-the-controller convention.</summary>
    public bool IsVerified { get; set; }

    /// <summary>Populated only when <see cref="IsVerified"/> is true, from the approved verification's public-safe fields. Never carries the Government ID document, any private document path, phone, email, or address — those never leave the verification module.</summary>
    public string? SellerTypeDisplay { get; set; }

    public string? InstitutionName { get; set; }

    public IReadOnlyList<string> SellingCategoryDisplays { get; set; } = Array.Empty<string>();

    public DateTime? VerifiedAtUtc { get; set; }

    /// <summary>Reputation as a seller (BuyerToSeller reviews), stamped on by the controller the same way <see cref="IsVerified"/> is — see <c>MarketplaceController.Seller</c>.</summary>
    public double AverageRating { get; set; }

    public int ReviewCount { get; set; }
}
