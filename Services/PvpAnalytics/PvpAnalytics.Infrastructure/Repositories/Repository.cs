using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PvpAnalytics.Core.Repositories;

namespace PvpAnalytics.Infrastructure.Repositories;

public class Repository<TEntity>(PvpAnalyticsDbContext dbContext) : IRepository<TEntity>
    where TEntity : class
{
    private readonly DbSet<TEntity> _dbSet = dbContext.Set<TEntity>();

    public IQueryable<TEntity> Query() => _dbSet.AsNoTracking();

    public Task<TEntity?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        return _dbSet.FindAsync([id], ct).AsTask();
    }

    public async Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken ct = default)
    {
        return await _dbSet.AsNoTracking().ToListAsync(ct);
    }

    public async Task<IReadOnlyList<TEntity>> ListAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default)
    {
        return await _dbSet.AsNoTracking().Where(predicate).ToListAsync(ct);
    }

    public async Task<TEntity> AddAsync(TEntity entity, CancellationToken ct = default, bool autoSave = true)
    {
        await _dbSet.AddAsync(entity, ct);
        if (autoSave)
        {
            await dbContext.SaveChangesAsync(ct);
        }
        return entity;
    }

    public async Task UpdateAsync(TEntity entity, CancellationToken ct = default, bool autoSave = true)
    {
        _dbSet.Update(entity);
        if (autoSave)
        {
            await dbContext.SaveChangesAsync(ct);
        }
    }

    public async Task DeleteAsync(TEntity entity, CancellationToken ct = default, bool autoSave = true)
    {
        _dbSet.Remove(entity);
        if (autoSave)
        {
            await dbContext.SaveChangesAsync(ct);
        }
    }

    public async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default)
    {
        await _dbSet.AddRangeAsync(entities, ct);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default)
    {
        _dbSet.UpdateRange(entities);
        await dbContext.SaveChangesAsync(ct);
    }
}


