using System.Linq.Expressions;
using PaymentService.Core.Entities;
using PaymentService.Core.Repositories;

namespace PaymentService.Application.Services;

public class PaymentService(IRepository<Payment> repository) : ICrudService<Payment>
{
    public Task<Payment?> GetAsync(object[] keyValues, CancellationToken ct = default) => repository.GetByIdAsync(keyValues, ct);
    public Task<IReadOnlyList<Payment>> GetAllAsync(CancellationToken ct = default) => repository.ListAsync(ct);
    public Task<IReadOnlyList<Payment>> FindAsync(Expression<Func<Payment, bool>> predicate, CancellationToken ct = default) => repository.ListAsync(predicate, ct);
    public Task<Payment> CreateAsync(Payment entity, CancellationToken ct = default) => repository.AddAsync(entity, ct: ct);
    public Task UpdateAsync(Payment entity, CancellationToken ct = default) => repository.UpdateAsync(entity, ct: ct);
    public Task DeleteAsync(Payment entity, CancellationToken ct = default) => repository.DeleteAsync(entity, ct: ct);
}

