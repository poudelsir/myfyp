namespace SajhaSikshya.DTOs.AI;

/// <summary>AI-suggested price returned to the Create/Edit Listing form. Purely advisory — the seller applies it explicitly via "Apply Suggested Price"; nothing here overwrites <see cref="Data.Entities.Marketplace.Listing.Price"/> automatically.</summary>
public class ListingPriceRecommendationDto
{
    public decimal SuggestedPrice { get; set; }

    public decimal SuggestedMinPrice { get; set; }

    public decimal SuggestedMaxPrice { get; set; }

    /// <summary>"Low", "Medium", or "High" — enforced by Gemini's response schema enum, not re-validated here.</summary>
    public string Confidence { get; set; } = string.Empty;

    public string Explanation { get; set; } = string.Empty;
}
