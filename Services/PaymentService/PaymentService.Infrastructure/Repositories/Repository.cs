using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PaymentService.Core.Repositories;

namespace PaymentService.Infrastructure.Repositories;

public class Repository<TEntity>(PaymentDbContext dbContext) : IRepository<TEntity>
    where TEntity : class
{
    private readonly DbSet<TEntity> _dbSet = dbContext.Set<TEntity>();

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
        // Check if entity is already tracked (from GetByIdAsync/FindAsync)
        var entry = dbContext.Entry(entity);
        if (entry.State == EntityState.Detached)
        {
            // Entity not tracked - attach and mark as modified
            _dbSet.Attach(entity);
            entry.State = EntityState.Modified;
        }
        // If already tracked, EF Core will detect property changes automatically
        // No need to mark entire entity as modified - only changed properties will be saved
        
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
