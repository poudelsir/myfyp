namespace SajhaSikshya.DTOs;

/// <summary>Non-generic pagination metadata, so a single Razor partial (<c>Views/Shared/_Pagination.cshtml</c>) can render any <see cref="PagedResult{T}"/> without needing a generic view model.</summary>
public interface IPagedResult
{
    int PageNumber { get; }

    int PageSize { get; }

    int TotalCount { get; }

    int TotalPages { get; }

    bool HasPreviousPage { get; }

    bool HasNextPage { get; }
}

/// <summary>
/// A single page of results plus the metadata needed to render pagination
/// controls. Returned by <see cref="Repositories.Interfaces.IGenericRepository{TEntity}.GetPagedAsync"/>
/// and threaded through Services up to Admin list views, so every paginated
/// screen in the app (present and future) shares one shape.
/// </summary>
public class PagedResult<T> : IPagedResult
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 10;

    public int TotalCount { get; init; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPreviousPage => PageNumber > 1;

    public bool HasNextPage => PageNumber < TotalPages;
}
