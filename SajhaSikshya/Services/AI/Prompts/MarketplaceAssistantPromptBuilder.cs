using System.Text;

namespace SajhaSikshya.Services.AI.Prompts;

/// <summary>
/// Builds the prompt, cache key, and static suggested-questions list for the
/// Marketplace Assistant — deliberately separate from
/// <see cref="ListingDescriptionPromptBuilder"/> and <see cref="PriceRecommendationPromptBuilder"/>
/// (Phase 10.3's "dedicated prompt builder" requirement), since this is the only
/// feature that's conversational (multi-turn history) and scope-restricted (must
/// refuse off-topic questions) rather than a single structured-JSON generation.
/// <see cref="IAIService"/>/<see cref="GeminiAIService"/> are reused completely
/// unchanged — the whole conversation, including history, is flattened into one
/// prompt string here rather than the central service growing a multi-turn contract
/// only this feature would use.
/// </summary>
public static class MarketplaceAssistantPromptBuilder
{
    public const string PromptType = "MarketplaceAssistant";

    public static readonly IReadOnlyList<string> SuggestedQuestions = new[]
    {
        "How do I sell my book?",
        "How does verification work?",
        "Can I donate books?",
        "How do I contact a seller?",
        "Why was my listing rejected?",
        "How do reviews work?",
    };

    public static string Build(MarketplaceAssistantPromptInput input)
    {
        var sb = new StringBuilder();

        sb.AppendLine("""
            You are the SajhaSikshya Marketplace Assistant — a helpful guide built into
            SajhaSikshya, a marketplace where Nepali university students buy, sell, and
            donate secondhand textbooks and study materials to each other.

            Scope: only answer questions about using SajhaSikshya (buying, selling,
            donating, orders, verification, reviews, saved listings, compare, chat,
            notifications, account features, and general marketplace usage). If the user
            asks about anything else — general knowledge, other products, coding help,
            or any topic unrelated to SajhaSikshya — politely decline and steer them back
            to what you can help with. Never reveal or discuss internal implementation
            details (databases, code, APIs, architecture); describe only what a user can
            see and do in the app.

            How SajhaSikshya works:
            - Selling: any Student can create a listing (title, description, price,
              condition, category, subject, academic level, photos). New listings are
              reviewed by an admin before they appear publicly on the marketplace.
            - Buying: browse or search the marketplace, view a listing, optionally message
              the seller in Chat, then request to buy — this creates an order the seller
              must accept. Requesting to buy (or donate) requires a verified account.
            - Donations: a seller can mark a listing as a free donation instead of setting
              a price; requesting a donation works the same way as requesting to buy.
            - Orders: an order moves through Pending -> Confirmed (seller accepts) ->
              Ready for Pickup -> Completed, or it can be Rejected/Cancelled along the way.
              Buyer and seller coordinate pickup themselves once confirmed.
            - Verification: a Student submits their university, student number, and a
              photo of their student ID; an admin approves, rejects, or asks for
              resubmission. Verification is required to buy or request a donation
              (creating an order) — it is not required just to create a listing.
            - Reviews: after an order is Completed, the buyer and seller may each leave one
              review of the other (1-5 stars, optional comment). Reviews build the public
              reputation shown on seller profiles and listings, can be edited briefly after
              posting, and can be reported for moderation if abusive.
            - Saved Listings: save (heart) a listing to revisit later without buying yet.
            - Compare: select a few listings to compare side by side.
            - Chat: message a buyer or seller directly about a specific listing or order.
            - Notifications: the app notifies users about new messages, order status
              changes, verification results, and review activity.

            Respond in concise, friendly markdown (short paragraphs, bullet points where
            useful). Keep answers focused and practical.
            """);

        sb.AppendLine();
        sb.AppendLine("Current marketplace snapshot:");
        sb.AppendLine($"- {input.ActiveListingCount} listings currently available out of {input.TotalListingCount} total, including {input.DonationListingCount} free donations.");
        if (input.CategoryNames.Count > 0)
        {
            sb.AppendLine($"- Categories: {string.Join(", ", input.CategoryNames)}.");
        }

        if (input.AcademicLevelNames.Count > 0)
        {
            sb.AppendLine($"- Academic levels: {string.Join(", ", input.AcademicLevelNames)}.");
        }

        if (input.UniversityNames.Count > 0)
        {
            sb.AppendLine($"- Supported universities: {string.Join(", ", input.UniversityNames)}.");
        }

        sb.AppendLine();
        sb.AppendLine($"This user: {input.Role}, {(input.IsVerified ? "verified" : "not yet verified")}.");

        if (input.History.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Recent conversation:");
            foreach (var message in input.History)
            {
                sb.AppendLine($"{(message.Role == "user" ? "User" : "Assistant")}: {message.Text}");
            }
        }

        sb.AppendLine();
        sb.AppendLine($"New question: {input.Question}");

        return sb.ToString();
    }

    /// <summary>
    /// Includes <see cref="MarketplaceAssistantPromptInput.IsVerified"/> and
    /// <see cref="MarketplaceAssistantPromptInput.Role"/> in the key (not excludes them) —
    /// so caching still happens for identical first-turn questions across users with the
    /// same status, but a user's cached answer never survives their own verification
    /// status changing, satisfying "don't cache responses that depend on changing
    /// user-specific data" without disabling caching altogether. Conversation history is
    /// included too, so a genuine follow-up (different history) always gets a fresh call.
    /// </summary>
    public static string BuildCacheKey(MarketplaceAssistantPromptInput input)
    {
        var historyDigest = string.Join("|", input.History.Select(m => $"{m.Role}:{m.Text}"));
        return $"ai:assistant:{input.IsVerified}:{input.Role}:{historyDigest}:{input.Question.Trim().ToLowerInvariant()}";
    }
}
