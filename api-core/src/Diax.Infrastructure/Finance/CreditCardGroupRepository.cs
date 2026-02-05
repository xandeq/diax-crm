using Diax.Domain.Finance;
using Diax.Infrastructure.Data;
using Diax.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Finance;

public class CreditCardGroupRepository : Repository<CreditCardGroup>, ICreditCardGroupRepository
{
    public CreditCardGroupRepository(DiaxDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<CreditCardGroup>> GetActiveGroupsAsync()
    {
        return await Context.CreditCardGroups
            .Where(g => g.IsActive)
            .OrderBy(g => g.Name)
            .ToListAsync();
    }

    public async Task<CreditCardGroup?> GetByIdWithCardsAsync(Guid id)
    {
        return await Context.CreditCardGroups
            .Include(g => g.Cards)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<CreditCardGroup?> GetByIdWithInvoicesAsync(Guid id)
    {
        return await Context.CreditCardGroups
            .Include(g => g.Invoices)
            .ThenInclude(i => i.Expenses)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<IEnumerable<CreditCardGroup>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await Context.CreditCardGroups
            .Include(g => g.Cards)
            .Where(x => x.UserId == userId)
            .ToListAsync(ct);
    }

    public async Task<CreditCardGroup?> GetByIdAndUserAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        return await Context.CreditCardGroups
            .Include(g => g.Cards)
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
    }
}
