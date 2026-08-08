using System.Linq.Expressions;
using SajhaSikshya.Data.Entities;
using SajhaSikshya.DTOs;

namespace SajhaSikshya.Repositories.Interfaces;

/// <summary>
/// Generic data-access contract for <see cref="BaseEntity"/>-derived entities.
/// Controllers and Services never talk to EF Core's <c>DbContext</c> directly —
/// every query and mutation goes through a repository, which is the only place
/// SQL/LINQ-to-Entities is allowed to live.
/// </summary>
/// <typeparam name="TEntity">A domain entity inheriting from <see cref="BaseEntity"/>.</typeparam>
public interface IGenericRepository<TEntity> where TEntity : BaseEntity
{
    Task<TEntity?> GetByIdAsync(int id);

    /// <summary>
    /// Same as <see cref="GetByIdAsync"/> but bypasses the soft-delete query filter, so a
    /// soft-deleted row can still be found and restored (e.g. a Restore action). Never
    /// use this for normal reads — only for the specific "undo delete" code path.
    /// </summary>
    Task<TEntity?> GetByIdIncludingDeletedAsync(int id);

    /// <summary>
    /// Same idea as <see cref="GetByIdIncludingDeletedAsync"/> but keyed by an arbitrary
    /// predicate instead of just Id — for entities identified by a composite natural key
    /// (e.g. a <c>SavedListing</c>'s UserId+ListingId) where a caller needs to know
    /// whether a soft-deleted row already exists before deciding whether to insert a new
    /// one or restore the old one. Same caution as <see cref="GetByIdIncludingDeletedAsync"/>:
    /// only for that specific "undo a soft-delete" style code path, not normal reads.
    /// </summary>
    Task<TEntity?> FirstOrDefaultIncludingDeletedAsync(Expression<Func<TEntity, bool>> predicate);

    Task<IReadOnlyList<TEntity>> GetAllAsync();

    Task<IReadOnlyList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    /// <paramref name="include"/> lets callers eager-load navigation properties for a
    /// single-row lookup (e.g. Listing.Seller/Category/Subject) without leaking
    /// IQueryable out of the repository — same rationale as <see cref="GetPagedAsync"/>.
    /// </summary>
    Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null);

    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate);

    /// <summary>Counts matching rows without loading them — for dashboard stats and similar, where <see cref="FindAsync"/> would waste a full materialization just to call .Count.</summary>
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? filter = null);

    /// <summary>
    /// SQL-side <c>SUM(...)</c> over matching rows — for money/quantity aggregates (e.g.
    /// completed order revenue) where <see cref="FindProjectedAsync{TResult}"/>'s bounded
    /// row cap would silently under-count on a large dataset. Returns 0 for an empty
    /// result set, matching <c>Enumerable.Sum</c>'s own semantics.
    /// </summary>
    Task<decimal> SumAsync(Expression<Func<TEntity, bool>>? filter, Expression<Func<TEntity, decimal>> selector);

    /// <summary>
    /// Returns one page of results. <paramref name="include"/> lets callers eager-load
    /// navigation properties (e.g. Subject.AcademicLevel) without leaking IQueryable
    /// out of the repository; <paramref name="orderBy"/> must be supplied since paging
    /// over an unordered set gives inconsistent results across pages.
    /// </summary>
    Task<PagedResult<TEntity>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<TEntity, bool>>? filter,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null);

    /// <summary>
    /// Same paging behavior as <see cref="GetPagedAsync"/>, but bypasses the soft-delete
    /// query filter — the paged sibling of <see cref="GetByIdIncludingDeletedAsync"/>,
    /// for an admin moderation queue that needs to list both live and previously-removed
    /// rows together (e.g. so a removed review can be found again and Restored). Same
    /// caution as the single-row version: only for that specific moderation-queue use
    /// case, never for a normal list.
    /// </summary>
    Task<PagedResult<TEntity>> GetPagedIncludingDeletedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<TEntity, bool>>? filter,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null);

    /// <summary>
    /// Same paging behavior as <see cref="GetPagedAsync"/>, but projects straight to
    /// <typeparamref name="TResult"/> in the SQL query via <paramref name="selector"/>
    /// instead of materializing full <typeparamref name="TEntity"/> graphs — for
    /// list/card views that only need a handful of columns (e.g. a listing card doesn't
    /// need the full Listing entity with every navigation property loaded).
    /// <paramref name="selector"/> must only reference translatable member access —
    /// no C# method calls (e.g. no enum .GetDisplayName(), no custom ToString()); do
    /// any of that formatting after materialization instead.
    /// </summary>
    Task<PagedResult<TResult>> GetPagedProjectedAsync<TResult>(
        int pageNumber,
        int pageSize,
        Expression<Func<TEntity, bool>>? filter,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy,
        Expression<Func<TEntity, TResult>> selector);

    /// <summary>Non-paged sibling of <see cref="GetPagedProjectedAsync{TResult}"/>, for small bounded lists (e.g. "4 related listings", "8 featured listings").</summary>
    Task<IReadOnlyList<TResult>> FindProjectedAsync<TResult>(
        Expression<Func<TEntity, bool>> filter,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy,
        int take,
        Expression<Func<TEntity, TResult>> selector);

    /// <summary>
    /// Same paged/projected shape as <see cref="GetPagedProjectedAsync{TResult}"/>, but
    /// <paramref name="filter"/> takes a queryable-shaping pipeline instead of a single
    /// <c>Expression&lt;Func&lt;TEntity,bool&gt;&gt;</c> — callers can chain multiple
    /// <c>.Where()</c> calls (e.g. one per search keyword, each ANDed with the last) or
    /// build conditional filters imperatively, which a single expression can't do as
    /// cleanly. Exists specifically for <see cref="Services.Marketplace.ListingSearchService"/>;
    /// <see cref="GetPagedProjectedAsync{TResult}"/> remains the right choice for the
    /// simpler single-expression filters used elsewhere.
    /// </summary>
    Task<PagedResult<TResult>> SearchPagedProjectedAsync<TResult>(
        int pageNumber,
        int pageSize,
        Func<IQueryable<TEntity>, IQueryable<TEntity>> filter,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy,
        Expression<Func<TEntity, TResult>> selector);

    Task AddAsync(TEntity entity);

    void Update(TEntity entity);

    /// <summary>
    /// Soft-deletes the entity by setting <see cref="BaseEntity.IsDeleted"/> rather
    /// than removing the row, preserving audit history.
    /// </summary>
    void Remove(TEntity entity);
}
