using SajhaSikshya.Data;
using SajhaSikshya.Data.Entities;
using SajhaSikshya.Repositories.Interfaces;

namespace SajhaSikshya.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IUnitOfWork"/>. Caches one repository instance
/// per entity type per request (via the scoped DbContext) and exposes a single
/// <see cref="SaveChangesAsync"/> that commits everything tracked in that request.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly Dictionary<Type, object> _repositories = new();
    private bool _disposed;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IGenericRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity
    {
        var entityType = typeof(TEntity);

        if (_repositories.TryGetValue(entityType, out var existing))
        {
            return (IGenericRepository<TEntity>)existing;
        }

        var repository = new GenericRepository<TEntity>(_context);
        _repositories[entityType] = repository;
        return repository;
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _context.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
