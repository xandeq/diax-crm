using Diax.Domain.Common;

namespace Diax.Domain.Finance;

public interface IIncomeCategoryRepository : IRepository<IncomeCategory>
{
    Task<IEnumerable<IncomeCategory>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<IncomeCategory>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<IncomeCategory?> GetByIdAndUserAsync(Guid id, Guid userId, CancellationToken ct = default);
}
