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

    public async Task<IEnumerable<CreditCard>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await DbSet
            .Where(x => x.UserId == userId)
            .ToListAsync(ct);
    }

    public async Task<CreditCard?> GetByIdAndUserAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
    }
}
