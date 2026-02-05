using Diax.Domain.Common;

namespace Diax.Domain.Finance;

public interface IIncomeRepository : IRepository<Income>
{
    Task<IEnumerable<Income>> GetByMonthAsync(int year, int month, CancellationToken cancellationToken = default);
    Task<IEnumerable<Income>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Income?> GetByIdAndUserAsync(Guid id, Guid userId, CancellationToken ct = default);
}
