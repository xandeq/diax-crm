using Diax.Domain.Common;

namespace Diax.Domain.Finance;

public interface IExpenseRepository : IRepository<Expense>
{
    Task<IEnumerable<Expense>> GetByMonthAsync(int year, int month, CancellationToken cancellationToken = default);
    Task<IEnumerable<Expense>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Expense?> GetByIdAndUserAsync(Guid id, Guid userId, CancellationToken ct = default);
}
