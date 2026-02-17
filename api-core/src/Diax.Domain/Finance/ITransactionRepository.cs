using Diax.Domain.Common;

namespace Diax.Domain.Finance;

public interface ITransactionRepository : IRepository<Transaction>
{
    Task<IEnumerable<Transaction>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Transaction?> GetByIdAndUserAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<IEnumerable<Transaction>> GetByTypeAsync(TransactionType type, Guid userId, CancellationToken ct = default);
    Task<IEnumerable<Transaction>> GetByMonthAsync(int year, int month, Guid userId, CancellationToken ct = default);
    Task<IEnumerable<Transaction>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, Guid userId, CancellationToken ct = default);
    Task<IEnumerable<Transaction>> GetByAccountIdAsync(Guid financialAccountId, Guid userId, CancellationToken ct = default);
    Task<IEnumerable<Transaction>> GetByTransferGroupIdAsync(Guid transferGroupId, CancellationToken ct = default);
}
