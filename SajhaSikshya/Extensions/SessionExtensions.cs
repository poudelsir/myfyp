using System.Text.Json;
using Microsoft.AspNetCore.Http;
using SajhaSikshya.Constants;
using SajhaSikshya.Services.AI;

namespace SajhaSikshya.Extensions;

/// <summary>
/// Guest comparison state lives entirely in session (see Milestone 4.3's spec: "Guests:
/// Comparison list stored in Session"), never the database — this is the one place that
/// serialization lives, so <see cref="Controllers.CompareController"/> and
/// <see cref="Controllers.MarketplaceController"/> (which both need to read it — one to
/// mutate it, one to stamp <c>IsInCompare</c> on cards) don't duplicate the encoding.
/// The Marketplace Assistant's short conversation history (Phase 10.3) follows the same
/// reasoning: it's inherently ephemeral and explicitly bounded ("avoid unbounded context
/// growth"), so Session — not a database table — is the right home for it too.
/// </summary>
public static class SessionExtensions
{
    private const string CompareListingIdsKey = "compare-listing-ids";
    private const string AssistantHistoryKey = "assistant-history";

    public static IReadOnlyList<int> GetCompareListingIds(this ISession session)
    {
        var json = session.GetString(CompareListingIdsKey);
        if (string.IsNullOrEmpty(json))
        {
            return Array.Empty<int>();
        }

        return JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>();
    }

    public static void SetCompareListingIds(this ISession session, IReadOnlyList<int> listingIds)
    {
        session.SetString(CompareListingIdsKey, JsonSerializer.Serialize(listingIds));
    }

    public static void ClearCompareListingIds(this ISession session)
    {
        session.Remove(CompareListingIdsKey);
    }

    public static IReadOnlyList<AssistantMessage> GetAssistantHistory(this ISession session)
    {
        var json = session.GetString(AssistantHistoryKey);
        if (string.IsNullOrEmpty(json))
        {
            return Array.Empty<AssistantMessage>();
        }

        return JsonSerializer.Deserialize<List<AssistantMessage>>(json) ?? new List<AssistantMessage>();
    }

    /// <summary>
    /// Appends one user/assistant exchange and trims to the last
    /// <see cref="AIConstants.MaxAssistantHistoryExchanges"/> exchanges — the one place
    /// growth is capped, so every caller automatically gets a bounded rolling window
    /// instead of needing to remember to trim itself.
    /// </summary>
    public static void AppendAssistantExchange(this ISession session, string userText, string assistantText)
    {
        var history = session.GetAssistantHistory().ToList();
        history.Add(new AssistantMessage("user", userText));
        history.Add(new AssistantMessage("assistant", assistantText));

        var maxMessages = AIConstants.MaxAssistantHistoryExchanges * 2;
        if (history.Count > maxMessages)
        {
            history = history.Skip(history.Count - maxMessages).ToList();
        }

        session.SetString(AssistantHistoryKey, JsonSerializer.Serialize(history));
    }

    public static void ClearAssistantHistory(this ISession session)
    {
        session.Remove(AssistantHistoryKey);
    }
}
