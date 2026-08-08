namespace SajhaSikshya.DTOs.Reviews;

/// <summary>
/// A user's reputation, computed live from their non-deleted <see cref="Data.Entities.Reviews.Review"/>
/// rows rather than a maintained/denormalized projection — see
/// <see cref="Services.Reviews.ReviewQueryService.GetReputationAsync"/>'s remarks on why.
/// Seller and Buyer reputations are tracked completely independently since the same
/// user can be either party across different orders.
/// </summary>
public class ReputationDto
{
    public double SellerAverageRating { get; set; }

    public int SellerReviewCount { get; set; }

    /// <summary>Index 0 = count of 1-star reviews received as a seller, ... index 4 = count of 5-star.</summary>
    public IReadOnlyList<int> SellerRatingDistribution { get; set; } = new int[5];

    public double BuyerAverageRating { get; set; }

    public int BuyerReviewCount { get; set; }

    public IReadOnlyList<int> BuyerRatingDistribution { get; set; } = new int[5];
}
