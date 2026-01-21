using Microsoft.EntityFrameworkCore;
using TechnicalTest.Data.Models;

namespace TechnicalTest.Data.Repositories;

public interface IRepository<T> where T : BaseEntity
{
    IQueryable<T> GetQueryable();
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public abstract class BaseRepository<T>(ApplicationContext context) : IRepository<T> where T : BaseEntity
{
    protected abstract DbSet<T> GetDbSet();

    public IQueryable<T> GetQueryable()
    {
        return GetDbSet().Where(e => e.DeletedAt == null);
    }

    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        entity.CreatedAt = DateTime.UtcNow;
        
        var result = await GetDbSet().AddAsync(entity, cancellationToken);
        return result.Entity;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return context.SaveChangesAsync(cancellationToken);
    }
}