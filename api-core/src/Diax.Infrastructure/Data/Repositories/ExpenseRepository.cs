using Diax.Domain.Finance;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class ExpenseRepository : Repository<Expense>, IExpenseRepository
{
    public ExpenseRepository(DiaxDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Expense>> GetByMonthAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(e => e.Date.Year == year && e.Date.Month == month)
            .ToListAsync(cancellationToken);
    }
}
