using Microsoft.AspNetCore.Http;
using SajhaSikshya.Constants;
using SajhaSikshya.Data.Entities.Catalog;
using SajhaSikshya.Data.Entities.Marketplace;
using SajhaSikshya.Data.Enums;
using SajhaSikshya.Data.ValueObjects;
using SajhaSikshya.Repositories.Interfaces;
using SajhaSikshya.Services.Interfaces;
using SajhaSikshya.Services.Interfaces.Marketplace;
using SajhaSikshya.Services.Interfaces.Notifications;
using SajhaSikshya.Services.Notifications;
using SajhaSikshya.ViewModels.Marketplace;

namespace SajhaSikshya.Services.Marketplace;

/// <summary>
/// Implements <see cref="IListingService"/>. <see cref="ModerateListingAsync"/> also
/// notifies the seller (Phase 8.2) of the outcome via <see cref="_notificationService"/> —
/// centralized in that one method since every admin moderation action already funnels
/// through it, the same reasoning that already centralizes the moderation-trail stamp
/// there.
/// </summary>
public class ListingService : IListingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IImageStorageService _imageStorageService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ListingService> _logger;

    public ListingService(
        IUnitOfWork unitOfWork,
        IImageStorageService imageStorageService,
        INotificationService notificationService,
        ILogger<ListingService> logger)
    {
        _unitOfWork = unitOfWork;
        _imageStorageService = imageStorageService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<ServiceResult<int>> CreateAsync(string sellerId, ListingFormViewModel model)
    {
        var subject = await _unitOfWork.Repository<Subject>().GetByIdAsync(model.SubjectId);
        if (subject is null || !subject.IsActive)
        {
            return ServiceResult<int>.Failure("The selected subject does not exist.");
        }

        if (!await _unitOfWork.Repository<Category>().AnyAsync(c => c.Id == model.CategoryId && c.IsActive))
        {
            return ServiceResult<int>.Failure("The selected category does not exist.");
        }

        var priceValidationError = ValidatePrice(model.PriceAmount, model.IsDonation);
        if (priceValidationError is not null)
        {
            return ServiceResult<int>.Failure(priceValidationError);
        }

        if (model.StockQuantity < 1)
        {
            return ServiceResult<int>.Failure("Please enter an initial stock quantity of at least 1.");
        }

        var listingRepository = _unitOfWork.Repository<Listing>();
        var slug = await Slug.MakeUniqueAsync(model.Title, async candidate =>
            await listingRepository.AnyAsync(l => l.Slug == candidate));

        var listing = new Listing
        {
            Title = model.Title.Trim(),
            Slug = slug.Value,
            Description = model.Description.Trim(),
            Price = new Money(model.IsDonation ? 0m : model.PriceAmount),
            IsDonation = model.IsDonation,
            Condition = model.Condition,
            // Straight to PendingApproval, not Draft — there is no separate "save as
            // draft" action anywhere in the seller UI (Create has one Save button), so
            // every submission is, in practice, a submission for review.
            Status = ListingStatus.PendingApproval,
            StockQuantity = model.StockQuantity,
            SellerId = sellerId,
            CategoryId = model.CategoryId,
            SubjectId = model.SubjectId,
            AcademicLevelId = subject.AcademicLevelId,
            UniversityId = subject.UniversityId,
        };

        await listingRepository.AddAsync(listing);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult<int>.Success(listing.Id);
    }

    public async Task<ServiceResult> UpdateAsync(string sellerId, ListingFormViewModel model)
    {
        var listing = await GetOwnedListingAsync(sellerId, model.Id);
        if (listing is null)
        {
            return ServiceResult.Failure("Listing not found.");
        }

        if (listing.Status is ListingStatus.Sold or ListingStatus.Reserved or ListingStatus.Donated)
        {
            return ServiceResult.Failure("This listing can no longer be edited because it has already been reserved, sold, or donated.");
        }

        if (listing.Status == ListingStatus.Archived)
        {
            return ServiceResult.Failure("This listing is archived. Restore it before editing.");
        }

        var subject = await _unitOfWork.Repository<Subject>().GetByIdAsync(model.SubjectId);
        if (subject is null || !subject.IsActive)
        {
            return ServiceResult.Failure("The selected subject does not exist.");
        }

        if (!await _unitOfWork.Repository<Category>().AnyAsync(c => c.Id == model.CategoryId && c.IsActive))
        {
            return ServiceResult.Failure("The selected category does not exist.");
        }

        var priceValidationError = ValidatePrice(model.PriceAmount, model.IsDonation);
        if (priceValidationError is not null)
        {
            return ServiceResult.Failure(priceValidationError);
        }

        listing.Title = model.Title.Trim();
        listing.Description = model.Description.Trim();
        listing.Price = new Money(model.IsDonation ? 0m : model.PriceAmount);
        listing.IsDonation = model.IsDonation;
        listing.Condition = model.Condition;
        listing.CategoryId = model.CategoryId;
        listing.SubjectId = model.SubjectId;
        listing.AcademicLevelId = subject.AcademicLevelId;
        listing.UniversityId = subject.UniversityId;
        listing.StockQuantity = model.StockQuantity;

        // Edited content has to be reviewed again before it's live; a Draft that was
        // never submitted yet simply stays a Draft. OutOfStock is included here too —
        // otherwise editing an out-of-stock listing's content would skip re-moderation,
        // and a later restock would silently republish unreviewed content as Active.
        if (listing.Status is ListingStatus.Active or ListingStatus.Rejected or ListingStatus.OutOfStock)
        {
            listing.Status = ListingStatus.PendingApproval;
        }

        _unitOfWork.Repository<Listing>().Update(listing);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeleteAsync(string sellerId, int id)
    {
        var listing = await GetOwnedListingAsync(sellerId, id);
        if (listing is null)
        {
            return ServiceResult.Failure("Listing not found.");
        }

        var result = DeleteInternal(listing);
        if (result.Succeeded)
        {
            await _unitOfWork.SaveChangesAsync();
        }

        return result;
    }

    public async Task<ServiceResult> ArchiveAsync(string sellerId, int id)
    {
        var listing = await GetOwnedListingAsync(sellerId, id);
        if (listing is null)
        {
            return ServiceResult.Failure("Listing not found.");
        }

        var result = ArchiveInternal(listing);
        if (result.Succeeded)
        {
            await _unitOfWork.SaveChangesAsync();
        }

        return result;
    }

    public async Task<ServiceResult> RestoreAsync(string sellerId, int id)
    {
        var repository = _unitOfWork.Repository<Listing>();
        var listing = await repository.GetByIdIncludingDeletedAsync(id);

        if (listing is null || listing.SellerId != sellerId)
        {
            return ServiceResult.Failure("Listing not found.");
        }

        var result = RestoreInternal(listing);
        if (result.Succeeded)
        {
            await _unitOfWork.SaveChangesAsync();
        }

        return result;
    }

    public async Task<ServiceResult> UpdateStockAsync(string sellerId, int id, int newStockQuantity)
    {
        var listing = await GetOwnedListingAsync(sellerId, id);
        if (listing is null)
        {
            return ServiceResult.Failure("Listing not found.");
        }

        if (listing.Status is not (ListingStatus.Active or ListingStatus.OutOfStock))
        {
            return ServiceResult.Failure("Stock can only be updated while a listing is active or out of stock.");
        }

        var error = ApplyStockChange(listing, newStockQuantity);
        if (error is not null)
        {
            return ServiceResult.Failure(error);
        }

        _unitOfWork.Repository<Listing>().Update(listing);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> AdminUpdateStockAsync(int id, int newStockQuantity)
    {
        var listing = await _unitOfWork.Repository<Listing>().GetByIdAsync(id);
        if (listing is null)
        {
            return ServiceResult.Failure("Listing not found.");
        }

        if (listing.Status is not (ListingStatus.Active or ListingStatus.OutOfStock))
        {
            return ServiceResult.Failure("Stock can only be updated while a listing is active or out of stock.");
        }

        var error = ApplyStockChange(listing, newStockQuantity);
        if (error is not null)
        {
            return ServiceResult.Failure(error);
        }

        _unitOfWork.Repository<Listing>().Update(listing);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> ModerateListingAsync(int listingId, ModerationAction action, string moderatorId, string? reason = null)
    {
        var repository = _unitOfWork.Repository<Listing>();

        // Restore is the only action that needs to see a soft-deleted row — every other
        // action operates on a listing that's still "live" from the repository's point of view.
        var listing = action == ModerationAction.Restore
            ? await repository.GetByIdIncludingDeletedAsync(listingId)
            : await repository.GetByIdAsync(listingId);

        if (listing is null)
        {
            return ServiceResult.Failure("Listing not found.");
        }

        var result = action switch
        {
            ModerationAction.Approve => ApproveInternal(listing),
            ModerationAction.Reject => RejectInternal(listing),
            ModerationAction.Archive => ArchiveInternal(listing),
            ModerationAction.Restore => RestoreInternal(listing),
            ModerationAction.Delete => DeleteInternal(listing),
            _ => ServiceResult.Failure("Unrecognized moderation action."),
        };

        if (!result.Succeeded)
        {
            return result;
        }

        // Every admin action funnels through here, so the moderation trail is stamped in
        // exactly one place regardless of which action ran above — the point of centralizing
        // this instead of repeating it in five separate Admin*Async methods.
        listing.LastModerationAction = action;
        listing.LastModeratedByUserId = moderatorId;
        listing.LastModeratedAtUtc = DateTime.UtcNow;
        listing.ModerationReason = reason;
        repository.Update(listing);

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Listing {ListingId} moderated: {Action} by {ModeratorId}", listingId, action, moderatorId);

        // Delete has no notification — the spec's Marketplace integration list is
        // Approved/Rejected/Archived/Restored only; a seller-deleted-by-admin listing
        // simply disappears, the same as a seller deleting their own.
        var listingNotification = action switch
        {
            ModerationAction.Approve => NotificationTemplates.ListingApproved(listing.Title),
            ModerationAction.Reject => NotificationTemplates.ListingRejected(listing.Title, reason),
            ModerationAction.Archive => NotificationTemplates.ListingArchived(listing.Title),
            ModerationAction.Restore => NotificationTemplates.ListingRestored(listing.Title),
            _ => ((string Title, string Message)?)null,
        };

        if (listingNotification is not null)
        {
            var link = $"/Student/Listings/Edit/{listing.Id}";
            await _notificationService.CreateAsync(listing.SellerId, NotificationType.Listing, listingNotification.Value.Title, listingNotification.Value.Message, link, createdBy: moderatorId);
        }

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> UploadImagesAsync(string sellerId, int listingId, IReadOnlyList<IFormFile> files)
    {
        var listing = await GetOwnedListingAsync(sellerId, listingId);
        if (listing is null)
        {
            return ServiceResult.Failure("Listing not found.");
        }

        if (files.Count == 0)
        {
            return ServiceResult.Failure("Please choose at least one image to upload.");
        }

        var imageRepository = _unitOfWork.Repository<ListingImage>();
        var existingImages = await imageRepository.FindAsync(i => i.ListingId == listingId);

        if (existingImages.Count + files.Count > ListingConstants.MaximumImages)
        {
            return ServiceResult.Failure(
                $"A listing can have at most {ListingConstants.MaximumImages} images " +
                $"(this would add {files.Count} to the existing {existingImages.Count}).");
        }

        var subfolder = $"listings/{listingId}";
        var savedPaths = new List<string>();
        var nextDisplayOrder = existingImages.Count == 0 ? 0 : existingImages.Max(i => i.DisplayOrder) + 1;
        var newImages = new List<ListingImage>();

        foreach (var file in files)
        {
            var saveResult = await _imageStorageService.SaveAsync(
                file, subfolder, ListingConstants.AllowedImageExtensions, ListingConstants.MaximumImageSizeMB);

            if (!saveResult.Succeeded)
            {
                // One bad file in the batch rolls back every file already saved earlier in
                // this same call — no database rows were written yet, so this is enough to
                // leave zero trace of the failed attempt.
                await RollbackSavedFilesAsync(savedPaths);
                return ServiceResult.Failure(saveResult.Errors.FirstOrDefault() ?? "One of the images could not be uploaded.");
            }

            savedPaths.Add(saveResult.Data!);

            var image = new ListingImage
            {
                ListingId = listingId,
                ImagePath = saveResult.Data!,
                DisplayOrder = nextDisplayOrder++,
            };

            newImages.Add(image);
            await imageRepository.AddAsync(image);
        }

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save {Count} uploaded image records for listing {ListingId}", newImages.Count, listingId);
            await RollbackSavedFilesAsync(savedPaths);
            return ServiceResult.Failure("Failed to save the uploaded images. Please try again.");
        }

        // First images ever uploaded for this listing: auto-assign the first one as thumbnail.
        if (listing.ThumbnailImageId is null && newImages.Count > 0)
        {
            await SetThumbnailInternalAsync(listing, newImages[0]);
        }

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeleteImageAsync(string sellerId, int imageId)
    {
        var imageRepository = _unitOfWork.Repository<ListingImage>();
        var image = await imageRepository.GetByIdAsync(imageId);
        if (image is null)
        {
            return ServiceResult.Failure("Image not found.");
        }

        var listing = await GetOwnedListingAsync(sellerId, image.ListingId);
        if (listing is null)
        {
            return ServiceResult.Failure("Image not found.");
        }

        var wasThumbnail = listing.ThumbnailImageId == image.Id;
        var imagePath = image.ImagePath;

        if (wasThumbnail)
        {
            // Keep IsThumbnail accurate even on the row being soft-deleted — it's
            // unreachable through any query once IsDeleted is set, but leaving it stale
            // would misrepresent history if this row is ever inspected directly.
            image.IsThumbnail = false;
        }

        imageRepository.Remove(image);

        if (wasThumbnail)
        {
            var remainingImages = (await imageRepository.FindAsync(i => i.ListingId == listing.Id && i.Id != image.Id))
                .OrderBy(i => i.DisplayOrder)
                .ToList();

            var nextThumbnail = remainingImages.FirstOrDefault();
            listing.ThumbnailImageId = nextThumbnail?.Id;

            if (nextThumbnail is not null)
            {
                nextThumbnail.IsThumbnail = true;
                imageRepository.Update(nextThumbnail);
            }

            _unitOfWork.Repository<Listing>().Update(listing);
        }

        await _unitOfWork.SaveChangesAsync();

        // The row is soft-deleted for audit consistency (matching every other entity in
        // the app), but the physical file is removed immediately — there is no "restore
        // image" feature, so an orphaned file on disk after this point would serve no purpose.
        await _imageStorageService.DeleteAsync(imagePath);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> SetThumbnailAsync(string sellerId, int imageId)
    {
        var imageRepository = _unitOfWork.Repository<ListingImage>();
        var image = await imageRepository.GetByIdAsync(imageId);
        if (image is null)
        {
            return ServiceResult.Failure("Image not found.");
        }

        var listing = await GetOwnedListingAsync(sellerId, image.ListingId);
        if (listing is null)
        {
            return ServiceResult.Failure("Image not found.");
        }

        await SetThumbnailInternalAsync(listing, image);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> ReplaceImageAsync(string sellerId, int imageId, IFormFile file)
    {
        var imageRepository = _unitOfWork.Repository<ListingImage>();
        var image = await imageRepository.GetByIdAsync(imageId);
        if (image is null)
        {
            return ServiceResult.Failure("Image not found.");
        }

        var listing = await GetOwnedListingAsync(sellerId, image.ListingId);
        if (listing is null)
        {
            return ServiceResult.Failure("Image not found.");
        }

        var saveResult = await _imageStorageService.SaveAsync(
            file, $"listings/{image.ListingId}", ListingConstants.AllowedImageExtensions, ListingConstants.MaximumImageSizeMB);

        if (!saveResult.Succeeded)
        {
            return ServiceResult.Failure(saveResult.Errors.FirstOrDefault() ?? "The replacement image could not be uploaded.");
        }

        var oldPath = image.ImagePath;
        image.ImagePath = saveResult.Data!;

        try
        {
            imageRepository.Update(image);
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save replacement image record for ListingImage {ImageId}", imageId);
            await _imageStorageService.DeleteAsync(saveResult.Data!);
            return ServiceResult.Failure("Failed to replace the image. Please try again.");
        }

        // The old file is only removed once the new path is safely committed — if
        // anything above had failed, the original file would still be intact.
        await _imageStorageService.DeleteAsync(oldPath);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> ReorderImagesAsync(string sellerId, int listingId, IReadOnlyList<int> orderedImageIds)
    {
        var listing = await GetOwnedListingAsync(sellerId, listingId);
        if (listing is null)
        {
            return ServiceResult.Failure("Listing not found.");
        }

        var imageRepository = _unitOfWork.Repository<ListingImage>();
        var images = await imageRepository.FindAsync(i => i.ListingId == listingId);
        var imagesById = images.ToDictionary(i => i.Id);

        if (orderedImageIds.Count != images.Count || orderedImageIds.Any(id => !imagesById.ContainsKey(id)))
        {
            return ServiceResult.Failure("The image order does not match this listing's images.");
        }

        for (var index = 0; index < orderedImageIds.Count; index++)
        {
            var image = imagesById[orderedImageIds[index]];
            if (image.DisplayOrder != index)
            {
                image.DisplayOrder = index;
                imageRepository.Update(image);
            }
        }

        await _unitOfWork.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> IncrementViewCountAsync(int listingId)
    {
        var repository = _unitOfWork.Repository<Listing>();
        var listing = await repository.GetByIdAsync(listingId);
        if (listing is null)
        {
            return ServiceResult.Failure("Listing not found.");
        }

        listing.ViewCount++;
        repository.Update(listing);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Success();
    }

    /// <summary>Clears the flag on whichever image currently claims the thumbnail (if any) so exactly one ListingImage.IsThumbnail is ever true per listing, then assigns it to <paramref name="newThumbnail"/>.</summary>
    private async Task SetThumbnailInternalAsync(Listing listing, ListingImage newThumbnail)
    {
        var imageRepository = _unitOfWork.Repository<ListingImage>();

        if (listing.ThumbnailImageId.HasValue && listing.ThumbnailImageId != newThumbnail.Id)
        {
            var previousThumbnail = await imageRepository.GetByIdAsync(listing.ThumbnailImageId.Value);
            if (previousThumbnail is not null)
            {
                previousThumbnail.IsThumbnail = false;
                imageRepository.Update(previousThumbnail);
            }
        }

        newThumbnail.IsThumbnail = true;
        imageRepository.Update(newThumbnail);

        listing.ThumbnailImageId = newThumbnail.Id;
        _unitOfWork.Repository<Listing>().Update(listing);

        await _unitOfWork.SaveChangesAsync();
    }

    private async Task RollbackSavedFilesAsync(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            await _imageStorageService.DeleteAsync(path);
        }
    }

    /// <summary>Fetches a listing only if it exists and belongs to <paramref name="sellerId"/>; used by every seller-facing mutation except Restore (which must also see soft-deleted rows).</summary>
    private async Task<Listing?> GetOwnedListingAsync(string sellerId, int id)
    {
        var listing = await _unitOfWork.Repository<Listing>().GetByIdAsync(id);
        return listing is not null && listing.SellerId == sellerId ? listing : null;
    }

    /// <summary>
    /// Shared soft-delete mechanics for the seller-owned <see cref="DeleteAsync"/> and the
    /// admin <see cref="ModerateListingAsync"/> paths — no business rule to enforce here
    /// beyond "it exists". Mutates the entity only; callers are responsible for calling
    /// <see cref="IUnitOfWork.SaveChangesAsync"/> once they've applied any additional
    /// changes (e.g. moderation metadata) on top of this.
    /// </summary>
    private ServiceResult DeleteInternal(Listing listing)
    {
        _unitOfWork.Repository<Listing>().Remove(listing);
        return ServiceResult.Success();
    }

    /// <summary>Shared archive-eligibility rule for the seller-owned <see cref="ArchiveAsync"/> and admin <see cref="ModerateListingAsync"/> paths — kept in one place so the Sold/Donated guard can't drift out of sync between them. Does not save.</summary>
    private ServiceResult ArchiveInternal(Listing listing)
    {
        if (listing.Status is ListingStatus.Sold or ListingStatus.Donated)
        {
            return ServiceResult.Failure("This listing cannot be archived because it has already been sold or donated.");
        }

        listing.Status = ListingStatus.Archived;
        _unitOfWork.Repository<Listing>().Update(listing);

        return ServiceResult.Success();
    }

    /// <summary>Shared restore mechanics for the seller-owned <see cref="RestoreAsync"/> and admin <see cref="ModerateListingAsync"/> paths — undoes either a soft-delete or an Archive, always sending the listing back through review. Does not save.</summary>
    private ServiceResult RestoreInternal(Listing listing)
    {
        if (!listing.IsDeleted && listing.Status != ListingStatus.Archived)
        {
            return ServiceResult.Failure("This listing is not deleted or archived.");
        }

        listing.IsDeleted = false;
        listing.Status = ListingStatus.PendingApproval;
        _unitOfWork.Repository<Listing>().Update(listing);

        return ServiceResult.Success();
    }

    /// <summary>Admin-only action (no seller-facing equivalent exists) — validated and applied by <see cref="ModerateListingAsync"/>. Does not save.</summary>
    private ServiceResult ApproveInternal(Listing listing)
    {
        if (listing.Status != ListingStatus.PendingApproval)
        {
            return ServiceResult.Failure("Only listings pending approval can be approved.");
        }

        listing.Status = ListingStatus.Active;
        _unitOfWork.Repository<Listing>().Update(listing);

        return ServiceResult.Success();
    }

    /// <summary>Admin-only action (no seller-facing equivalent exists) — validated and applied by <see cref="ModerateListingAsync"/>. Does not save.</summary>
    private ServiceResult RejectInternal(Listing listing)
    {
        if (listing.Status != ListingStatus.PendingApproval)
        {
            return ServiceResult.Failure("Only listings pending approval can be rejected.");
        }

        listing.Status = ListingStatus.Rejected;
        _unitOfWork.Repository<Listing>().Update(listing);

        return ServiceResult.Success();
    }

    /// <summary>
    /// Shared stock-quantity validation and auto Active/OutOfStock flip, used by
    /// <see cref="UpdateStockAsync"/> and <see cref="AdminUpdateStockAsync"/> — the single
    /// place a restock or manual zero-out recomputes visibility, so the two entry points
    /// can't drift out of sync. Does not save.
    /// </summary>
    private static string? ApplyStockChange(Listing listing, int newStockQuantity)
    {
        if (newStockQuantity < ListingConstants.MinimumStockQuantity || newStockQuantity > ListingConstants.MaximumStockQuantity)
        {
            return $"Stock quantity must be between {ListingConstants.MinimumStockQuantity} and {ListingConstants.MaximumStockQuantity}.";
        }

        listing.StockQuantity = newStockQuantity;

        if (listing.Status == ListingStatus.OutOfStock && newStockQuantity > 0)
        {
            listing.Status = ListingStatus.Active;
        }
        else if (listing.Status == ListingStatus.Active && newStockQuantity == 0)
        {
            listing.Status = ListingStatus.OutOfStock;
        }

        return null;
    }

    private static string? ValidatePrice(decimal priceAmount, bool isDonation)
    {
        if (isDonation)
        {
            return null;
        }

        if (priceAmount < ListingConstants.MinimumPrice || priceAmount > ListingConstants.MaximumPrice)
        {
            return $"Price must be between {ListingConstants.MinimumPrice:N0} and {ListingConstants.MaximumPrice:N0}.";
        }

        return null;
    }
}
