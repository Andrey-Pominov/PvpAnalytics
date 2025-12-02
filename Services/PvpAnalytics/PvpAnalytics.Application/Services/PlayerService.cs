using System.Linq.Expressions;
using PvpAnalytics.Core.Entities;
using PvpAnalytics.Core.Repositories;

namespace PvpAnalytics.Application.Services;

public class PlayerService(IRepository<Player> repository) : ICrudService<Player>
{
    public Task<Player?> GetAsync(long id, CancellationToken ct = default) => repository.GetByIdAsync(id, ct);
    public Task<IReadOnlyList<Player>> GetAllAsync(CancellationToken ct = default) => repository.ListAsync(ct);
    public Task<IReadOnlyList<Player>> FindAsync(Expression<Func<Player, bool>> predicate, CancellationToken ct = default) => repository.ListAsync(predicate, ct);
    public Task<Player> CreateAsync(Player entity, CancellationToken ct = default) => repository.AddAsync(entity, true, ct);
    public Task UpdateAsync(Player entity, CancellationToken ct = default) => repository.UpdateAsync(entity, true, ct);
    public Task DeleteAsync(Player entity, CancellationToken ct = default) => repository.DeleteAsync(entity, true, ct);
}


