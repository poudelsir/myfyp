namespace SajhaSikshya.Constants;

/// <summary>
/// Default pagination sizing shared by every paginated list in the app (Admin catalog
/// screens today; Listing search/browse tomorrow), so "10 per page" and "clamp at 100"
/// are asserted once instead of being retyped as bare numbers in each repository call
/// or controller.
/// </summary>
public static class PaginationConstants
{
    /// <summary>Page size used when a caller doesn't specify one.</summary>
    public const int DefaultPageSize = 10;

    /// <summary>
    /// Hard ceiling on page size regardless of what a caller requests, so a crafted
    /// query string (e.g. <c>?pageSize=100000</c>) can't force an unbounded query.
    /// </summary>
    public const int MaximumPageSize = 100;

    /// <summary>Sort direction assumed when a list doesn't specify one ("asc" or "desc").</summary>
    public const string DefaultSortOrder = "asc";
}
