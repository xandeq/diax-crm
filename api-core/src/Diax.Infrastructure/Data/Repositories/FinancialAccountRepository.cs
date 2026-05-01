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

    public async Task<IEnumerable<FinancialAccount>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await DbSet
            .Where(x => x.UserId == userId)
            .OrderBy(a => a.Name)
            .ToListAsync(ct);
    }

    public async Task<FinancialAccount?> GetByIdAndUserAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
    }
}
