using SajhaSikshya.Data.Enums;
using SajhaSikshya.DTOs;
using SajhaSikshya.DTOs.Marketplace;

namespace SajhaSikshya.ViewModels.Admin.Marketplace;

/// <summary>
/// Backs the Admin listing moderation queue. A dedicated type rather than reusing
/// <see cref="SajhaSikshya.ViewModels.Admin.Shared.AdminListViewModel{TItem}"/> — this
/// screen needs a status filter alongside search, which the generic Catalog list
/// wrapper doesn't have (and adding it there would be a needless change to a type
/// several other, unrelated Admin screens already depend on).
/// </summary>
public class AdminListingListViewModel
{
    public PagedResult<ListingDto> Page { get; set; } = new();

    public string? SearchTerm { get; set; }

    public ListingStatus? Status { get; set; }
}
