using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace SajhaSikshya.Extensions;

/// <summary>
/// Guest comparison state lives entirely in session (see Milestone 4.3's spec: "Guests:
/// Comparison list stored in Session"), never the database — this is the one place that
/// serialization lives, so <see cref="Controllers.CompareController"/> and
/// <see cref="Controllers.MarketplaceController"/> (which both need to read it — one to
/// mutate it, one to stamp <c>IsInCompare</c> on cards) don't duplicate the encoding.
/// </summary>
public static class SessionExtensions
{
    private const string CompareListingIdsKey = "compare-listing-ids";

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
}
