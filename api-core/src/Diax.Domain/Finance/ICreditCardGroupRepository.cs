using Diax.Domain.Common;

namespace Diax.Domain.Finance;

public interface ICreditCardGroupRepository : IRepository<CreditCardGroup>
{
    Task<IEnumerable<CreditCardGroup>> GetActiveGroupsAsync();
    Task<IEnumerable<CreditCardGroup>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<CreditCardGroup?> GetByIdAndUserAsync(Guid id, Guid userId, CancellationToken ct = default);
}
