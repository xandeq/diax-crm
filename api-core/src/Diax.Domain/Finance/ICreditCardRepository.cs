using Diax.Domain.Common;

namespace Diax.Domain.Finance;

public interface ICreditCardRepository : IRepository<CreditCard>
{
    Task<CreditCard?> GetByLastFourDigitsAsync(string lastFourDigits, CancellationToken cancellationToken = default);
}
