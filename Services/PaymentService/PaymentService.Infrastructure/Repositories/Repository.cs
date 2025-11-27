using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PaymentService.Core.Repositories;

namespace PaymentService.Infrastructure.Repositories;

public class Repository<TEntity>(PaymentDbContext dbContext) : IRepository<TEntity>
    where TEntity : class
{
    private readonly DbSet<TEntity> _dbSet = dbContext.Set<TEntity>();

    public Task<TEntity?> GetByIdAsync(object[] keyValues, CancellationToken ct = default)
    {
        return _dbSet.FindAsync(keyValues, ct).AsTask();
    }

    public async Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken ct = default)
    {
        return await _dbSet.AsNoTracking().ToListAsync(ct);
    }

    public async Task<IReadOnlyList<TEntity>> ListAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default)
    {
        return await _dbSet.AsNoTracking().Where(predicate).ToListAsync(ct);
    }

    public IQueryable<TEntity> GetQueryable()
    {
        return _dbSet.AsNoTracking();
    }

    public async Task<(IReadOnlyList<TEntity> Items, int Total)> GetPagedAsync(
        IQueryable<TEntity> query,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        
        return (items, total);
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
        var entry = dbContext.Entry(entity);
        if (entry.State == EntityState.Detached)
        {
            _dbSet.Attach(entity);
            entry.State = EntityState.Modified;
        }
        
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

    public async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default, bool autoSave = true)
    {
        await _dbSet.AddRangeAsync(entities, ct);
        if (autoSave)
        {
            await dbContext.SaveChangesAsync(ct);
        }
    }

    public async Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default, bool autoSave = true)
    {
        _dbSet.UpdateRange(entities);
        if (autoSave)
        {
            await dbContext.SaveChangesAsync(ct);
        }
    }
}
