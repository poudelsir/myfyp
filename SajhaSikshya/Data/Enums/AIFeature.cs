using System.ComponentModel.DataAnnotations;

namespace SajhaSikshya.Data.Enums;

/// <summary>
/// Which platform feature triggered an AI call — recorded on every <see cref="Data.Entities.AI.AIUsageLog"/>
/// row so usage can be broken down per feature in Admin AI Insights. Only the
/// feature built so far has a member; Phase 10.2/10.3/10.4 add
/// PriceRecommendation/MarketplaceAssistant/AdminInsights the same way
/// <see cref="NotificationType"/> grew a member per phase rather than declaring
/// the whole set upfront.
/// </summary>
public enum AIFeature
{
    [Display(Name = "Listing Description Generator", Description = "AI-generated title, description, and keywords for a new or edited listing.")]
    ListingDescriptionGenerator = 0,

    [Display(Name = "Price Recommendation", Description = "AI-suggested selling price, price range, and confidence for a listing.")]
    PriceRecommendation = 1,

    [Display(Name = "Marketplace Assistant", Description = "Conversational Q&A about buying, selling, donating, and using SajhaSikshya.")]
    MarketplaceAssistant = 2,

    [Display(Name = "Admin Insights", Description = "AI-generated narrative summary of marketplace, order, verification, review, and platform activity for administrators.")]
    AdminInsights = 3,
}
