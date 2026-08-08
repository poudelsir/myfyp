namespace SajhaSikshya.Services.Interfaces.Marketplace;

/// <summary>
/// Owns the <c>CompareListing</c> entity's lifecycle — the *authenticated-user*
/// (database-backed) half of Milestone 4.3's comparison feature. Guest comparisons
/// never reach this service at all; they're handled entirely via
/// <see cref="Extensions.SessionExtensions"/> in <see cref="Controllers.CompareController"/>
/// and <see cref="Controllers.MarketplaceController"/>. Mirrors
/// <see cref="ISavedListingService"/>'s shape and restore-in-place toggle semantics —
/// a separate service rather than folded into it because a listing can independently be
/// saved, compared, both, or neither, and conflating the two would make neither
/// concern easy to reason about on its own.
/// </summary>
public interface ICompareService
{
    /// <summary>
    /// Adds <paramref name="listingId"/> to <paramref name="userId"/>'s comparison.
    /// Fails if the listing doesn't exist or isn't Active, if it's already in the
    /// comparison, or if the comparison is already at
    /// <see cref="Constants.SearchConstants.MaximumCompareCount"/>.
    /// </summary>
    Task<ServiceResult> AddAsync(string userId, int listingId);

    /// <summary>Removes <paramref name="listingId"/> from <paramref name="userId"/>'s comparison. A no-op (still succeeds) if it wasn't there.</summary>
    Task<ServiceResult> RemoveAsync(string userId, int listingId);

    /// <summary>Empties <paramref name="userId"/>'s entire comparison.</summary>
    Task<ServiceResult> ClearAsync(string userId);

    /// <summary>The listing ids currently in <paramref name="userId"/>'s comparison, most recently added first.</summary>
    Task<IReadOnlyList<int>> GetCompareListingIdsAsync(string userId);
}
