using Diax.Domain.Finance;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class AccountTransferRepository : Repository<AccountTransfer>, IAccountTransferRepository
{
    public AccountTransferRepository(DiaxDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<AccountTransfer>> GetByAccountIdAsync(Guid financialAccountId)
    {
        return await DbSet
            .Where(t => t.FromFinancialAccountId == financialAccountId || t.ToFinancialAccountId == financialAccountId)
            .Include(t => t.FromFinancialAccount)
            .Include(t => t.ToFinancialAccount)
            .OrderByDescending(t => t.Date)
            .ToListAsync();
    }

    public async Task<IEnumerable<AccountTransfer>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await DbSet
            .Where(t => t.Date >= startDate && t.Date <= endDate)
            .Include(t => t.FromFinancialAccount)
            .Include(t => t.ToFinancialAccount)
            .OrderByDescending(t => t.Date)
            .ToListAsync();
    }
}
