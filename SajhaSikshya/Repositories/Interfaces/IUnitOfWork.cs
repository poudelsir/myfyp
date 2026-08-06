using SajhaSikshya.Data.Entities;

namespace SajhaSikshya.Repositories.Interfaces;

/// <summary>
/// Coordinates one or more repository operations within a single EF Core change-tracking
/// scope and commits them atomically. Services depend on this instead of the DbContext
/// directly, so a single business operation that touches multiple repositories still
/// results in exactly one database transaction.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Returns the generic repository for the given entity type, creating and caching
    /// it on first use so repeated calls within the same unit of work share one instance.
    /// </summary>
    IGenericRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity;

    /// <summary>
    /// Persists all pending changes tracked across every repository obtained from this
    /// unit of work. Returns the number of state entries written to the database.
    /// </summary>
    Task<int> SaveChangesAsync();
}
