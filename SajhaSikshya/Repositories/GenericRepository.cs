using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SajhaSikshya.Data;
using SajhaSikshya.Data.Entities;
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

    public async Task<IReadOnlyList<TEntity>> GetAllAsync()
    {
        return await DbSet.Where(e => !e.IsDeleted).AsNoTracking().ToListAsync();
    }

    public async Task<IReadOnlyList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return await DbSet.Where(e => !e.IsDeleted).Where(predicate).AsNoTracking().ToListAsync();
    }

    public async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return await DbSet.Where(e => !e.IsDeleted).FirstOrDefaultAsync(predicate);
    }

    public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return await DbSet.Where(e => !e.IsDeleted).AnyAsync(predicate);
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
