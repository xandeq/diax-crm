using Diax.Domain.Common;

namespace Diax.Domain.Finance;

public interface ITransactionCategoryRepository : IRepository<TransactionCategory>
{
    Task<IEnumerable<TransactionCategory>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<TransactionCategory?> GetByIdAndUserAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<IEnumerable<TransactionCategory>> GetActiveAsync(Guid userId, CancellationToken ct = default);
    Task<IEnumerable<TransactionCategory>> GetByApplicableToAsync(CategoryApplicableTo applicableTo, Guid userId, CancellationToken ct = default);
}
