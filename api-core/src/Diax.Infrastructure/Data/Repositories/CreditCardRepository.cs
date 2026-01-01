using Diax.Domain.Finance;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class CreditCardRepository : Repository<CreditCard>, ICreditCardRepository
{
    public CreditCardRepository(DiaxDbContext context) : base(context)
    {
    }

    public async Task<CreditCard?> GetByLastFourDigitsAsync(string lastFourDigits, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(c => c.LastFourDigits == lastFourDigits, cancellationToken);
    }
}
