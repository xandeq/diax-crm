using Diax.Domain.Finance;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class IncomeCategoryRepository : Repository<IncomeCategory>, IIncomeCategoryRepository
{
    public IncomeCategoryRepository(DiaxDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<IncomeCategory>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<IncomeCategory>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await DbSet
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);
    }

    public async Task<IncomeCategory?> GetByIdAndUserAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
    }
}
