using System.Linq;
using System.Linq.Expressions;

namespace PaymentService.Core.Repositories;

public interface IRepository<TEntity> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TEntity>> ListAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
    Task<TEntity> AddAsync(TEntity entity, CancellationToken ct = default, bool autoSave = true);
    Task UpdateAsync(TEntity entity, CancellationToken ct = default, bool autoSave = true);
    Task DeleteAsync(TEntity entity, CancellationToken ct = default, bool autoSave = true);
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default, bool autoSave = true);
    Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default, bool autoSave = true);
    IQueryable<TEntity> GetQueryable();
    Task<(IReadOnlyList<TEntity> Items, int Total)> GetPagedAsync(
        IQueryable<TEntity> query,
        int page,
        int pageSize,
        CancellationToken ct = default);
}