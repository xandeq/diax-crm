using Diax.Domain.Common;

namespace Diax.Domain.Finance;

public interface IExpenseRepository : IRepository<Expense>
{
    Task<IEnumerable<Expense>> GetByMonthAsync(int year, int month, CancellationToken cancellationToken = default);
}
