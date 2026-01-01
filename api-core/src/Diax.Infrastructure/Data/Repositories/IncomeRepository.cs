using Diax.Domain.Finance;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class IncomeRepository : Repository<Income>, IIncomeRepository
{
    public IncomeRepository(DiaxDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Income>> GetByMonthAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(i => i.Date.Year == year && i.Date.Month == month)
            .ToListAsync(cancellationToken);
    }
}
