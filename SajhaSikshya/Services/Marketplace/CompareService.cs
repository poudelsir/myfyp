using SajhaSikshya.Constants;
using SajhaSikshya.Data.Entities.Marketplace;
using SajhaSikshya.Data.Enums;
using SajhaSikshya.Repositories.Interfaces;
using SajhaSikshya.Services.Interfaces.Marketplace;

namespace SajhaSikshya.Services.Marketplace;

public class CompareService : ICompareService
{
    private readonly IUnitOfWork _unitOfWork;

    public CompareService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult> AddAsync(string userId, int listingId)
    {
        var listingExists = await _unitOfWork.Repository<Listing>()
            .AnyAsync(l => l.Id == listingId && l.Status == ListingStatus.Active);

        if (!listingExists)
        {
            return ServiceResult.Failure("This listing is not available to compare.");
        }

        var repository = _unitOfWork.Repository<CompareListing>();

        // Same "look up ignoring the soft-delete filter" reasoning as SavedListingService —
        // a previously-removed row (IsDeleted=true) is restored in place instead of a
        // second row being inserted, which the UserId+ListingId unique index would reject.
        var existing = await repository.FirstOrDefaultIncludingDeletedAsync(
            c => c.UserId == userId && c.ListingId == listingId);

        if (existing is not null && !existing.IsDeleted)
        {
            return ServiceResult.Failure("This listing is already in your comparison.");
        }

        if (existing is null)
        {
            var currentCount = await repository.CountAsync(c => c.UserId == userId);
            if (currentCount >= SearchConstants.MaximumCompareCount)
            {
                return ServiceResult.Failure($"You can compare up to {SearchConstants.MaximumCompareCount} listings at a time. Remove one to add another.");
            }

            await repository.AddAsync(new CompareListing { UserId = userId, ListingId = listingId });
            await _unitOfWork.SaveChangesAsync();
            return ServiceResult.Success();
        }

        // existing is soft-deleted: restoring doesn't need the cap check again — it was
        // already counted while active and is coming right back to the same size.
        existing.IsDeleted = false;
        existing.CreatedAtUtc = DateTime.UtcNow;
        repository.Update(existing);
        await _unitOfWork.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> RemoveAsync(string userId, int listingId)
    {
        var repository = _unitOfWork.Repository<CompareListing>();
        var existing = await repository.FirstOrDefaultIncludingDeletedAsync(
            c => c.UserId == userId && c.ListingId == listingId);

        if (existing is not null && !existing.IsDeleted)
        {
            repository.Remove(existing);
            await _unitOfWork.SaveChangesAsync();
        }

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> ClearAsync(string userId)
    {
        var repository = _unitOfWork.Repository<CompareListing>();
        var items = await repository.FindAsync(c => c.UserId == userId);

        foreach (var item in items)
        {
            repository.Remove(item);
        }

        if (items.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync();
        }

        return ServiceResult.Success();
    }

    public async Task<IReadOnlyList<int>> GetCompareListingIdsAsync(string userId)
    {
        var repository = _unitOfWork.Repository<CompareListing>();
        var items = await repository.FindAsync(c => c.UserId == userId);

        return items
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => c.ListingId)
            .ToList();
    }
}
