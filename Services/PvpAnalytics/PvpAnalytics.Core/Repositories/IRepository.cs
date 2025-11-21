using System.Linq;
using System.Linq.Expressions;

namespace PvpAnalytics.Core.Repositories;

public interface IRepository<TEntity> where TEntity : class
{
    IQueryable<TEntity> Query();
    Task<TEntity?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TEntity>> ListAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
    Task<TEntity> AddAsync(TEntity entity, CancellationToken ct = default, bool autoSave = true);
    Task UpdateAsync(TEntity entity, CancellationToken ct = default, bool autoSave = true);
    Task DeleteAsync(TEntity entity, CancellationToken ct = default, bool autoSave = true);
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);
    Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);
}


