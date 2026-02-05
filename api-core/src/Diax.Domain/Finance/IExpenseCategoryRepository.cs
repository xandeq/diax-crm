using Diax.Domain.Common;

namespace Diax.Domain.Finance;

public interface IExpenseCategoryRepository : IRepository<ExpenseCategory>
{
    Task<IEnumerable<ExpenseCategory>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ExpenseCategory>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<ExpenseCategory?> GetByIdAndUserAsync(Guid id, Guid userId, CancellationToken ct = default);
}
