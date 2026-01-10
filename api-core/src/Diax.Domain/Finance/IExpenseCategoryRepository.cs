using Diax.Domain.Common;

namespace Diax.Domain.Finance;

public interface IExpenseCategoryRepository : IRepository<ExpenseCategory>
{
    Task<IEnumerable<ExpenseCategory>> GetActiveAsync(CancellationToken cancellationToken = default);
}
