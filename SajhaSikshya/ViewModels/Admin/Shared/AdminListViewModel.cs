using SajhaSikshya.DTOs;

namespace SajhaSikshya.ViewModels.Admin.Shared;

/// <summary>
/// Wraps a <see cref="PagedResult{T}"/> with the current search term so an Admin
/// list view can both render the page and re-populate the search box. Reused across
/// every Admin index screen instead of one bespoke wrapper per entity.
/// </summary>
public class AdminListViewModel<TItem>
{
    public PagedResult<TItem> Page { get; set; } = new();

    public string? SearchTerm { get; set; }
}
