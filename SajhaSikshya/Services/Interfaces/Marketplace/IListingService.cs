using Microsoft.AspNetCore.Http;
using SajhaSikshya.Data.Enums;
using SajhaSikshya.ViewModels.Marketplace;

namespace SajhaSikshya.Services.Interfaces.Marketplace;

/// <summary>
/// Listing mutations. Seller-scoped methods (Delete/Archive/Restore) take the acting
/// seller's id and verify ownership internally — a mismatch is reported as "not found"
/// rather than "forbidden" so a seller can't probe for the existence of another
/// seller's listing ids. <see cref="ModerateListingAsync"/> is the single,
/// deliberately separate entry point for every admin moderation action (Approve/
/// Reject/Archive/Restore/Delete) — kept apart from the seller-scoped methods (not a
/// shared method with an "isAdmin" flag) so the missing ownership check is obvious at
/// the call site, while still centralizing authorization (one method, one
/// [Authorize(Roles = Admin)] controller), validation, and moderation-metadata/audit
/// stamping across all five actions instead of duplicating it five times. Read paths
/// live in <see cref="IListingQueryService"/>.
/// </summary>
public interface IListingService
{
    Task<ServiceResult<int>> CreateAsync(string sellerId, ListingFormViewModel model);

    Task<ServiceResult> UpdateAsync(string sellerId, ListingFormViewModel model);

    /// <summary>Soft-deletes the listing. Not reachable back from "My Listings" in this milestone — there's no seller-facing trash view yet.</summary>
    Task<ServiceResult> DeleteAsync(string sellerId, int id);

    /// <summary>Sets the listing to <see cref="Data.Enums.ListingStatus.Archived"/> — reversible via <see cref="RestoreAsync"/>, unlike <see cref="DeleteAsync"/>.</summary>
    Task<ServiceResult> ArchiveAsync(string sellerId, int id);

    /// <summary>Undoes either a soft-delete or an archive, returning the listing to PendingApproval so it goes through review again before becoming Active.</summary>
    Task<ServiceResult> RestoreAsync(string sellerId, int id);

    /// <summary>
    /// Seller-scoped stock update — the "Restock" action and the general "update stock
    /// at any time" capability are the same call. Only allowed while the listing is
    /// Active or OutOfStock. Setting a positive quantity on an OutOfStock listing
    /// republishes it (back to Active) immediately, no admin approval; setting it to
    /// zero on an Active listing hides it (moves to OutOfStock).
    /// </summary>
    Task<ServiceResult> UpdateStockAsync(string sellerId, int id, int newStockQuantity);

    /// <summary>Admin-scoped equivalent of <see cref="UpdateStockAsync"/> — no ownership check, mirrors <see cref="ModerateListingAsync"/>'s unrestricted-by-design fetch-by-id pattern.</summary>
    Task<ServiceResult> AdminUpdateStockAsync(int id, int newStockQuantity);

    /// <summary>
    /// Validates and saves each file (delegating to <see cref="Interfaces.IImageStorageService"/>),
    /// enforcing <see cref="Constants.ListingConstants.MaximumImages"/> per listing. If any file in
    /// the batch fails, every file already saved earlier in the same call is deleted and no
    /// database rows are written — an upload either fully succeeds or leaves nothing behind.
    /// The first image ever uploaded for a listing is automatically made the thumbnail.
    /// </summary>
    Task<ServiceResult> UploadImagesAsync(string sellerId, int listingId, IReadOnlyList<IFormFile> files);

    /// <summary>
    /// Soft-deletes the image row and removes its physical file. If the deleted image was
    /// the thumbnail, the next remaining image (by display order) is automatically promoted;
    /// if none remain, the listing's ThumbnailImageId becomes null.
    /// </summary>
    Task<ServiceResult> DeleteImageAsync(string sellerId, int imageId);

    /// <summary>Marks <paramref name="imageId"/> as the listing's thumbnail, clearing the flag on whichever image previously held it.</summary>
    Task<ServiceResult> SetThumbnailAsync(string sellerId, int imageId);

    /// <summary>
    /// Swaps the file behind an existing <see cref="Data.Entities.Marketplace.ListingImage"/>
    /// row without changing its id, DisplayOrder, or IsThumbnail — unlike delete-then-upload,
    /// this preserves the image's position and thumbnail status. The new file is validated
    /// and saved first; the old physical file is only deleted after the database update
    /// commits successfully, so a mid-failure never loses both copies.
    /// </summary>
    Task<ServiceResult> ReplaceImageAsync(string sellerId, int imageId, IFormFile file);

    /// <summary>Re-numbers DisplayOrder to match <paramref name="orderedImageIds"/>, which must be exactly the listing's current image ids (in the new order).</summary>
    Task<ServiceResult> ReorderImagesAsync(string sellerId, int listingId, IReadOnlyList<int> orderedImageIds);

    /// <summary>
    /// Increments a listing's view count by one. Deliberately has no ownership check —
    /// any visitor viewing a public listing triggers this, not just the seller. Whether
    /// to call it at all (de-duplicating repeat views within one browser session) is
    /// decided by the caller (<c>MarketplaceController</c>), not this method.
    /// </summary>
    Task<ServiceResult> IncrementViewCountAsync(int listingId);

    /// <summary>
    /// Single entry point for every admin moderation action. Unrestricted (no ownership
    /// check — that's enforced by the caller's [Authorize(Roles = Admin)], not here) and
    /// unconditionally applied per-action:
    /// <list type="bullet">
    /// <item>Approve — moves a PendingApproval listing to Active. Fails for any other status.</item>
    /// <item>Reject — moves a PendingApproval listing to Rejected. Fails for any other status.</item>
    /// <item>Archive — same Sold/Donated guard as <see cref="ArchiveAsync"/>.</item>
    /// <item>Restore — same soft-delete/Archived precondition as <see cref="RestoreAsync"/>, always lands back in PendingApproval.</item>
    /// <item>Delete — same soft-delete mechanics as <see cref="DeleteAsync"/>.</item>
    /// </list>
    /// On success, stamps <c>LastModerationAction</c>/<c>LastModeratedByUserId</c>/
    /// <c>LastModeratedAtUtc</c>/<c>ModerationReason</c> on the listing — one place for
    /// this regardless of which action ran, instead of five separate methods each
    /// repeating it. <paramref name="reason"/> is optional and most meaningful on Reject.
    /// </summary>
    Task<ServiceResult> ModerateListingAsync(int listingId, ModerationAction action, string moderatorId, string? reason = null);
}
