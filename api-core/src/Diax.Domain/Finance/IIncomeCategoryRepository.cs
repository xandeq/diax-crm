using Diax.Domain.Common;

namespace Diax.Domain.Finance;

public interface IIncomeCategoryRepository : IRepository<IncomeCategory>
{
    Task<IEnumerable<IncomeCategory>> GetActiveAsync(CancellationToken cancellationToken = default);
}
