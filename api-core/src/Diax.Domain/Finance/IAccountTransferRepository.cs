using Diax.Domain.Common;

namespace Diax.Domain.Finance;

public interface IAccountTransferRepository : IRepository<AccountTransfer>
{
    Task<IEnumerable<AccountTransfer>> GetByAccountIdAsync(Guid financialAccountId);
    Task<IEnumerable<AccountTransfer>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
}
