using System.Linq.Expressions;
using PvpAnalytics.Core.Entities;
using PvpAnalytics.Core.Repositories;

namespace PvpAnalytics.Application.Services;

public class MatchService(IRepository<Match> repository) : ICrudService<Match>
{
    public Task<Match?> GetAsync(long id, CancellationToken ct = default) => repository.GetByIdAsync(id, ct);
    public Task<IReadOnlyList<Match>> GetAllAsync(CancellationToken ct = default) => repository.ListAsync(ct);
    public Task<IReadOnlyList<Match>> FindAsync(Expression<Func<Match, bool>> predicate, CancellationToken ct = default) => repository.ListAsync(predicate, ct);
    public Task<Match> CreateAsync(Match entity, CancellationToken ct = default) => repository.AddAsync(entity, ct);
    public Task UpdateAsync(Match entity, CancellationToken ct = default) => repository.UpdateAsync(entity, ct);
    public Task DeleteAsync(Match entity, CancellationToken ct = default) => repository.DeleteAsync(entity, ct);
}


