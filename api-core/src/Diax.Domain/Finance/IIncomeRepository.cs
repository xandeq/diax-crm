using Diax.Domain.Common;

namespace Diax.Domain.Finance;

public interface IIncomeRepository : IRepository<Income>
{
    Task<IEnumerable<Income>> GetByMonthAsync(int year, int month, CancellationToken cancellationToken = default);
}
