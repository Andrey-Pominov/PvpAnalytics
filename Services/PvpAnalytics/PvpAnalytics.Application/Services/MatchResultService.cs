using System.Linq.Expressions;
using PvpAnalytics.Core.Entities;
using PvpAnalytics.Core.Repositories;

namespace PvpAnalytics.Application.Services;

public class MatchResultService(IRepository<MatchResult> repository) : ICrudService<MatchResult>
{
    public Task<MatchResult?> GetAsync(long id, CancellationToken ct = default) => repository.GetByIdAsync(id, ct);
    public Task<IReadOnlyList<MatchResult>> GetAllAsync(CancellationToken ct = default) => repository.ListAsync(ct);
    public Task<IReadOnlyList<MatchResult>> FindAsync(Expression<Func<MatchResult, bool>> predicate, CancellationToken ct = default) => repository.ListAsync(predicate, ct);
    public Task<MatchResult> CreateAsync(MatchResult entity, CancellationToken ct = default) => repository.AddAsync(entity, ct: ct);
    public Task UpdateAsync(MatchResult entity, CancellationToken ct = default) => repository.UpdateAsync(entity, ct: ct);
    public Task DeleteAsync(MatchResult entity, CancellationToken ct = default) => repository.DeleteAsync(entity, ct: ct);
}


