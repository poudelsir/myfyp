using SajhaSikshya.Data.Entities.Marketplace;
using SajhaSikshya.Data.Enums;
using SajhaSikshya.Repositories.Interfaces;
using SajhaSikshya.Services.Interfaces.Marketplace;

namespace SajhaSikshya.Services.Marketplace;

public class SavedListingService : ISavedListingService
{
    private readonly IUnitOfWork _unitOfWork;

    public SavedListingService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult<bool>> ToggleSaveAsync(string userId, int listingId)
    {
        var listingExists = await _unitOfWork.Repository<Listing>()
            .AnyAsync(l => l.Id == listingId && l.Status == ListingStatus.Active);

        if (!listingExists)
        {
            return ServiceResult<bool>.Failure("This listing is not available to save.");
        }

        var repository = _unitOfWork.Repository<SavedListing>();

        // Looked up ignoring the soft-delete filter so a previously-unsaved row (still
        // present with IsDeleted=true) is found and restored in place rather than a
        // second row being inserted, which the UserId+ListingId unique index would reject.
        var existing = await repository.FirstOrDefaultIncludingDeletedAsync(
            s => s.UserId == userId && s.ListingId == listingId);

        if (existing is null)
        {
            await repository.AddAsync(new SavedListing { UserId = userId, ListingId = listingId });
            await _unitOfWork.SaveChangesAsync();
            return ServiceResult<bool>.Success(true);
        }

        if (existing.IsDeleted)
        {
            existing.IsDeleted = false;
            // Re-saving is a fresh save, not a resurrection of the old one — the "My
            // Saved Listings" page orders by CreatedAtUtc, and a listing someone unsaved
            // months ago and just saved again should show up as recently saved.
            existing.CreatedAtUtc = DateTime.UtcNow;
            repository.Update(existing);
            await _unitOfWork.SaveChangesAsync();
            return ServiceResult<bool>.Success(true);
        }

        repository.Remove(existing);
        await _unitOfWork.SaveChangesAsync();
        return ServiceResult<bool>.Success(false);
    }

    public async Task<IReadOnlySet<int>> GetSavedListingIdsAsync(string userId, IEnumerable<int> listingIds)
    {
        var ids = listingIds.ToList();
        if (ids.Count == 0)
        {
            return new HashSet<int>();
        }

        var repository = _unitOfWork.Repository<SavedListing>();
        var saved = await repository.FindAsync(s => s.UserId == userId && ids.Contains(s.ListingId));

        return saved.Select(s => s.ListingId).ToHashSet();
    }
}
