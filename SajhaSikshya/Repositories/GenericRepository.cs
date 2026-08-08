using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SajhaSikshya.Constants;
using SajhaSikshya.Data;
using SajhaSikshya.Data.Entities;
using SajhaSikshya.DTOs;
using SajhaSikshya.Repositories.Interfaces;

namespace SajhaSikshya.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IGenericRepository{TEntity}"/>. Every query
/// filters out soft-deleted rows by default so callers never have to remember to do it.
/// </summary>
public class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : BaseEntity
{
    protected readonly ApplicationDbContext Context;
    protected readonly DbSet<TEntity> DbSet;

    public GenericRepository(ApplicationDbContext context)
    {
        Context = context;
        DbSet = context.Set<TEntity>();
    }

    public async Task<TEntity?> GetByIdAsync(int id)
    {
        var entity = await DbSet.FindAsync(id);
        return entity is { IsDeleted: false } ? entity : null;
    }

    public async Task<TEntity?> GetByIdIncludingDeletedAsync(int id)
    {
        return await DbSet.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<TEntity?> FirstOrDefaultIncludingDeletedAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return await DbSet.IgnoreQueryFilters().FirstOrDefaultAsync(predicate);
    }

    public async Task<IReadOnlyList<TEntity>> GetAllAsync()
    {
        return await DbSet.Where(e => !e.IsDeleted).AsNoTracking().ToListAsync();
    }

    public async Task<IReadOnlyList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return await DbSet.Where(e => !e.IsDeleted).Where(predicate).AsNoTracking().ToListAsync();
    }

    public async Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null)
    {
        IQueryable<TEntity> query = DbSet.Where(e => !e.IsDeleted);

        if (include is not null)
        {
            query = include(query);
        }

        return await query.FirstOrDefaultAsync(predicate);
    }

    public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return await DbSet.Where(e => !e.IsDeleted).AnyAsync(predicate);
    }

    public async Task<int> CountAsync(Expression<Func<TEntity, bool>>? filter = null)
    {
        IQueryable<TEntity> query = DbSet.Where(e => !e.IsDeleted);

        if (filter is not null)
        {
            query = query.Where(filter);
        }

        return await query.CountAsync();
    }

    public async Task<decimal> SumAsync(Expression<Func<TEntity, bool>>? filter, Expression<Func<TEntity, decimal>> selector)
    {
        IQueryable<TEntity> query = DbSet.Where(e => !e.IsDeleted);

        if (filter is not null)
        {
            query = query.Where(filter);
        }

        return await query.SumAsync(selector);
    }

    public async Task<PagedResult<TEntity>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<TEntity, bool>>? filter,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null)
    {
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Clamp(pageSize, 1, PaginationConstants.MaximumPageSize);

        // The model-level query filter (HasQueryFilter) already excludes IsDeleted
        // rows here, including through Include()'d navigation properties.
        IQueryable<TEntity> query = DbSet.AsNoTracking();

        if (include is not null)
        {
            query = include(query);
        }

        if (filter is not null)
        {
            query = query.Where(filter);
        }

        var totalCount = await query.CountAsync();

        var items = await orderBy(query)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<TEntity>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<PagedResult<TEntity>> GetPagedIncludingDeletedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<TEntity, bool>>? filter,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null)
    {
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Clamp(pageSize, 1, PaginationConstants.MaximumPageSize);

        IQueryable<TEntity> query = DbSet.IgnoreQueryFilters().AsNoTracking();

        if (include is not null)
        {
            query = include(query);
        }

        if (filter is not null)
        {
            query = query.Where(filter);
        }

        var totalCount = await query.CountAsync();

        var items = await orderBy(query)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<TEntity>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<PagedResult<TResult>> GetPagedProjectedAsync<TResult>(
        int pageNumber,
        int pageSize,
        Expression<Func<TEntity, bool>>? filter,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy,
        Expression<Func<TEntity, TResult>> selector)
    {
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Clamp(pageSize, 1, PaginationConstants.MaximumPageSize);

        IQueryable<TEntity> query = DbSet.AsNoTracking();

        if (filter is not null)
        {
            query = query.Where(filter);
        }

        var totalCount = await query.CountAsync();

        var items = await orderBy(query)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(selector)
            .ToListAsync();

        return new PagedResult<TResult>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<IReadOnlyList<TResult>> FindProjectedAsync<TResult>(
        Expression<Func<TEntity, bool>> filter,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy,
        int take,
        Expression<Func<TEntity, TResult>> selector)
    {
        var query = DbSet.AsNoTracking().Where(e => !e.IsDeleted).Where(filter);
        return await orderBy(query).Take(take).Select(selector).ToListAsync();
    }

    public async Task<PagedResult<TResult>> SearchPagedProjectedAsync<TResult>(
        int pageNumber,
        int pageSize,
        Func<IQueryable<TEntity>, IQueryable<TEntity>> filter,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy,
        Expression<Func<TEntity, TResult>> selector)
    {
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Clamp(pageSize, 1, PaginationConstants.MaximumPageSize);

        var query = filter(DbSet.AsNoTracking());

        var totalCount = await query.CountAsync();

        var items = await orderBy(query)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(selector)
            .ToListAsync();

        return new PagedResult<TResult>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public async Task AddAsync(TEntity entity)
    {
        await DbSet.AddAsync(entity);
    }

    public void Update(TEntity entity)
    {
        entity.UpdatedAtUtc = DateTime.UtcNow;
        DbSet.Update(entity);
    }

    public void Remove(TEntity entity)
    {
        entity.IsDeleted = true;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        DbSet.Update(entity);
    }
}
