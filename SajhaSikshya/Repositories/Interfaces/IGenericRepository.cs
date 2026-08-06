using System.Linq.Expressions;
using SajhaSikshya.Data.Entities;

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

    Task<IReadOnlyList<TEntity>> GetAllAsync();

    Task<IReadOnlyList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);

    Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate);

    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate);

    Task AddAsync(TEntity entity);

    void Update(TEntity entity);

    /// <summary>
    /// Soft-deletes the entity by setting <see cref="BaseEntity.IsDeleted"/> rather
    /// than removing the row, preserving audit history.
    /// </summary>
    void Remove(TEntity entity);
}
