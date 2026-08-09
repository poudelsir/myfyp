using Microsoft.AspNetCore.Mvc.Rendering;
using SajhaSikshya.Data.Enums;
using SajhaSikshya.DTOs;
using SajhaSikshya.DTOs.Marketplace;

namespace SajhaSikshya.ViewModels.Student.Marketplace;

/// <summary>
/// Backs the seller's "My Listings" page. A dedicated type rather than reusing
/// <see cref="SajhaSikshya.ViewModels.Admin.Shared.AdminListViewModel{TItem}"/> — this
/// screen needs a status filter alongside search, mirroring why
/// <see cref="SajhaSikshya.ViewModels.Admin.Marketplace.AdminListingListViewModel"/>
/// exists as its own type instead of extending the shared generic.
/// </summary>
public class StudentListingListViewModel
{
    public PagedResult<ListingDto> Page { get; set; } = new();

    public string? SearchTerm { get; set; }

    public ListingStatus? Status { get; set; }

    public int? CategoryId { get; set; }

    public IEnumerable<SelectListItem> CategoryOptions { get; set; } = Array.Empty<SelectListItem>();
}
