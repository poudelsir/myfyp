namespace SajhaSikshya.Constants;

/// <summary>Tuning limits for marketplace search/discovery (Milestone 4), kept separate from <see cref="ListingConstants"/> since these govern query behavior rather than listing data itself.</summary>
public static class SearchConstants
{
    /// <summary>Search terms longer than this are truncated before use — a defensive ceiling against pathological query strings, not a real-world limit on what a keyword search needs.</summary>
    public const int MaximumSearchTermLength = 100;

    /// <summary>At most this many whitespace-separated words are used for relevance scoring/matching; extra words beyond this are ignored rather than growing the query unboundedly.</summary>
    public const int MaximumSearchWords = 8;

    /// <summary>Window, in days, that the "Recently Added" quick filter looks back from now.</summary>
    public const int RecentlyAddedDays = 7;

    /// <summary>Most listings a single comparison (guest session or authenticated user) may hold at once.</summary>
    public const int MaximumCompareCount = 4;
}
