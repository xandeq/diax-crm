using Diax.Domain.Finance;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class FinancialAccountRepository : Repository<FinancialAccount>, IFinancialAccountRepository
{
    public FinancialAccountRepository(DiaxDbContext context) : base(context)
    {
    }

    public new async Task<FinancialAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(a => a.Incomes)
            .Include(a => a.Expenses)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public new async Task<List<FinancialAccount>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<FinancialAccount>> GetActiveAccountsAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(a => a.IsActive)
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);
    }

    public new async Task AddAsync(FinancialAccount account, CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(account, cancellationToken);
    }

    public new Task UpdateAsync(FinancialAccount account, CancellationToken cancellationToken = default)
    {
        DbSet.Update(account);
        return Task.CompletedTask;
    }

    public new Task DeleteAsync(FinancialAccount account, CancellationToken cancellationToken = default)
    {
        DbSet.Remove(account);
        return Task.CompletedTask;
    }
}
