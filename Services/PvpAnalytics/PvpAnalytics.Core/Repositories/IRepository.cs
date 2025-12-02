using System.Linq;
using System.Linq.Expressions;

namespace PvpAnalytics.Core.Repositories;

public interface IRepository<TEntity> where TEntity : class
{
    /// <summary>
    /// Provides an <see cref="IQueryable{TEntity}"/> for composing custom queries.
    /// </summary>
    /// <returns>A no-tracking <see cref="IQueryable{TEntity}"/> that returns read-only, detached entities.</returns>
    /// <remarks>
    /// <para>
    /// Use <see cref="Query"/> when you need to compose complex or filtered queries that benefit from deferred execution
    /// and query composition. The returned queryable is configured with no-tracking, meaning entities are read-only
    /// and detached from the change tracker.
    /// </para>
    /// <para>
    /// For simple materialized lists, prefer <see cref="ListAsync()"/> or <see cref="ListAsync(Expression{Func{TEntity, bool}}, CancellationToken)"/>
    /// which return materialized collections directly.
    /// </para>
    /// <para>
    /// <strong>Caution:</strong> Exposing <see cref="IQueryable{TEntity}"/> allows callers to compose complex queries
    /// that may impact performance. Use judiciously and consider query complexity and database load.
    /// </para>
    /// </remarks>
    IQueryable<TEntity> Query();
    Task<TEntity?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TEntity>> ListAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
    Task<TEntity> AddAsync(TEntity entity, bool autoSave = true, CancellationToken ct = default);
    Task UpdateAsync(TEntity entity, bool autoSave = true, CancellationToken ct = default);
    Task DeleteAsync(TEntity entity, bool autoSave = true, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<TEntity> entities, bool autoSave = true, CancellationToken ct = default);
    Task UpdateRangeAsync(IEnumerable<TEntity> entities, bool autoSave = true, CancellationToken ct = default);
}


