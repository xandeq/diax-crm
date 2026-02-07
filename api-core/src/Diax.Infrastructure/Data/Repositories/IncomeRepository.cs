using Diax.Domain.Finance;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class IncomeRepository : Repository<Income>, IIncomeRepository
{
    public IncomeRepository(DiaxDbContext context) : base(context)
    {
    }

    public override async Task<Income?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(i => i.IncomeCategory)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public override async Task<IEnumerable<Income>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(i => i.IncomeCategory)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Income>> GetByMonthAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(i => i.IncomeCategory)
            .Where(i => i.Date.Year == year && i.Date.Month == month)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Income>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await DbSet
            .IgnoreQueryFilters()
            .Include(i => i.IncomeCategory)
            .Where(x => x.UserId == userId)
            .ToListAsync(ct);
    }

    public async Task<Income?> GetByIdAndUserAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        return await DbSet
            .IgnoreQueryFilters()
            .Include(i => i.IncomeCategory)
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
    }
}
