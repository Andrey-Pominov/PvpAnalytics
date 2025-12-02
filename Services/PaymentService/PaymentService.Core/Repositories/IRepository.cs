using System.Linq;
using System.Linq.Expressions;

namespace PaymentService.Core.Repositories;

public interface IRepository<TEntity> where TEntity : class
{
    /// <summary>
    /// Gets an entity by its primary key(s).
    /// </summary>
    /// <param name="keyValues">The primary key value(s). For composite keys, provide all key values in the order they are defined in the entity configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The entity if found, otherwise null.</returns>
    Task<TEntity?> GetByIdAsync(object[] keyValues, CancellationToken ct = default);
    Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TEntity>> ListAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
    Task<TEntity> AddAsync(TEntity entity, bool autoSave = true, CancellationToken ct = default);
    Task UpdateAsync(TEntity entity, bool autoSave = true, CancellationToken ct = default);
    Task DeleteAsync(TEntity entity, bool autoSave = true, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<TEntity> entities, bool autoSave = true, CancellationToken ct = default);
    Task UpdateRangeAsync(IEnumerable<TEntity> entities, bool autoSave = true, CancellationToken ct = default);
    IQueryable<TEntity> GetQueryable();
    Task<(IReadOnlyList<TEntity> Items, int Total)> GetPagedAsync(
        IQueryable<TEntity> query,
        int page,
        int pageSize,
        CancellationToken ct = default);
}