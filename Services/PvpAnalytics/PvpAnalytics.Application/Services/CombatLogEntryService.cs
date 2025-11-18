using System.Linq.Expressions;
using PvpAnalytics.Core.Entities;
using PvpAnalytics.Core.Repositories;

namespace PvpAnalytics.Application.Services;

public class CombatLogEntryService(IRepository<CombatLogEntry> repository) : ICrudService<CombatLogEntry>
{
    public Task<CombatLogEntry?> GetAsync(long id, CancellationToken ct = default) => repository.GetByIdAsync(id, ct);
    public Task<IReadOnlyList<CombatLogEntry>> GetAllAsync(CancellationToken ct = default) => repository.ListAsync(ct);
    public Task<IReadOnlyList<CombatLogEntry>> FindAsync(Expression<Func<CombatLogEntry, bool>> predicate, CancellationToken ct = default) => repository.ListAsync(predicate, ct);
    public Task<CombatLogEntry> CreateAsync(CombatLogEntry entity, CancellationToken ct = default) => repository.AddAsync(entity, ct);
    public Task UpdateAsync(CombatLogEntry entity, CancellationToken ct = default) => repository.UpdateAsync(entity, ct);
    public Task DeleteAsync(CombatLogEntry entity, CancellationToken ct = default) => repository.DeleteAsync(entity, ct);
}


