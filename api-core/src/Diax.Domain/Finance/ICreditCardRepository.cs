using Diax.Domain.Common;

namespace Diax.Domain.Finance;

public interface ICreditCardRepository : IRepository<CreditCard>
{
    Task<CreditCard?> GetByLastFourDigitsAsync(string lastFourDigits, CancellationToken cancellationToken = default);
    Task<IEnumerable<CreditCard>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<CreditCard?> GetByIdAndUserAsync(Guid id, Guid userId, CancellationToken ct = default);
}
