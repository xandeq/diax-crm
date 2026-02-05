using Diax.Domain.Common;

namespace Diax.Domain.Finance;

public interface IAccountTransferRepository : IRepository<AccountTransfer>
{
    Task<IEnumerable<AccountTransfer>> GetByAccountIdAsync(Guid financialAccountId);
    Task<IEnumerable<AccountTransfer>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<AccountTransfer>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<AccountTransfer?> GetByIdAndUserAsync(Guid id, Guid userId, CancellationToken ct = default);
}
