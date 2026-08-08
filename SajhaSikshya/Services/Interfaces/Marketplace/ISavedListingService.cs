namespace SajhaSikshya.Services.Interfaces.Marketplace;

/// <summary>
/// Owns the <c>SavedListing</c> entity's own lifecycle (a Student's "save for later"
/// bookmarks) — mutations and existence checks. Deliberately its own service rather
/// than folded into <see cref="IListingService"/> (which owns Listing mutations, a
/// different entity) or <see cref="IListingSearchService"/> (which is read-oriented;
/// it does own the *paged grid* of a user's saved listings — <c>GetMySavedListingsAsync</c>
/// — since that's a discovery-shaped read reusing the same projection as every other
/// browse grid). This service stays focused on the SavedListing rows themselves.
/// </summary>
public interface ISavedListingService
{
    /// <summary>
    /// Saves <paramref name="listingId"/> for <paramref name="userId"/> if not already
    /// saved, or un-saves it if it is — a single button/action doing both, rather than
    /// separate Save/Unsave endpoints the caller has to choose between. Fails if the
    /// listing doesn't exist or isn't <see cref="Data.Enums.ListingStatus.Active"/> (you
    /// can't bookmark something that isn't currently available). Returns the resulting
    /// saved state (<c>true</c> = now saved, <c>false</c> = now un-saved) so the caller
    /// can show the right feedback without a second query.
    /// </summary>
    Task<ServiceResult<bool>> ToggleSaveAsync(string userId, int listingId);

    /// <summary>
    /// Out of <paramref name="listingIds"/>, returns the subset currently saved by
    /// <paramref name="userId"/> — a single batched query (<c>WHERE UserId = @userId AND
    /// ListingId IN (...)</c>), used to stamp <c>ListingSummaryDto.IsSaved</c>/<c>ListingDto.IsSaved</c>
    /// across a whole page of cards without checking each one individually (avoids N+1).
    /// </summary>
    Task<IReadOnlySet<int>> GetSavedListingIdsAsync(string userId, IEnumerable<int> listingIds);
}
