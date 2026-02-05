using Diax.Domain.Finance;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class CreditCardInvoiceRepository : Repository<CreditCardInvoice>, ICreditCardInvoiceRepository
{
    public CreditCardInvoiceRepository(DiaxDbContext context) : base(context)
    {
    }

    public new async Task<CreditCardInvoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(i => i.CreditCardGroup)
            .ThenInclude(g => g.Cards)
            .Include(i => i.PaidFromAccount)
            .Include(i => i.Expenses)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public new async Task<List<CreditCardInvoice>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(i => i.CreditCardGroup)
            .OrderByDescending(i => i.ReferenceYear)
            .ThenByDescending(i => i.ReferenceMonth)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CreditCardInvoice>> GetByCreditCardIdAsync(Guid creditCardId, CancellationToken cancellationToken = default)
    {
        // Note: This method is kept for backward compatibility but now looks for invoices by group
        var card = await Context.CreditCards.FindAsync(new object[] { creditCardId }, cancellationToken);
        if (card?.CreditCardGroupId == null)
            return new List<CreditCardInvoice>();

        return await DbSet
            .Where(i => i.CreditCardGroupId == card.CreditCardGroupId)
            .Include(i => i.Expenses)
            .OrderByDescending(i => i.ReferenceYear)
            .ThenByDescending(i => i.ReferenceMonth)
            .ToListAsync(cancellationToken);
    }

    public async Task<CreditCardInvoice?> GetByCardAndPeriodAsync(Guid creditCardId, int month, int year, CancellationToken cancellationToken = default)
    {
        // Note: This method is kept for backward compatibility but now looks for invoices by group
        var card = await Context.CreditCards.FindAsync(new object[] { creditCardId }, cancellationToken);
        if (card?.CreditCardGroupId == null)
            return null;

        return await DbSet
            .Include(i => i.CreditCardGroup)
            .ThenInclude(g => g.Cards)
            .Include(i => i.Expenses)
            .FirstOrDefaultAsync(i => i.CreditCardGroupId == card.CreditCardGroupId
                && i.ReferenceMonth == month
                && i.ReferenceYear == year, cancellationToken);
    }

    public async Task<List<CreditCardInvoice>> GetByGroupIdAsync(Guid creditCardGroupId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(i => i.CreditCardGroupId == creditCardGroupId)
            .Include(i => i.Expenses)
            .OrderByDescending(i => i.ReferenceYear)
            .ThenByDescending(i => i.ReferenceMonth)
            .ToListAsync(cancellationToken);
    }

    public async Task<CreditCardInvoice?> GetByGroupAndPeriodAsync(Guid creditCardGroupId, int month, int year, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(i => i.CreditCardGroup)
            .ThenInclude(g => g.Cards)
            .Include(i => i.Expenses)
            .FirstOrDefaultAsync(i => i.CreditCardGroupId == creditCardGroupId
                && i.ReferenceMonth == month
                && i.ReferenceYear == year, cancellationToken);
    }

    public async Task<List<CreditCardInvoice>> GetUnpaidInvoicesAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(i => !i.IsPaid)
            .Include(i => i.CreditCardGroup)
            .ThenInclude(g => g.Cards)
            .OrderBy(i => i.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CreditCardInvoice>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await DbSet
            .Include(i => i.CreditCardGroup)
            .Where(x => x.UserId == userId)
            .OrderByDescending(i => i.ReferenceYear)
            .ThenByDescending(i => i.ReferenceMonth)
            .ToListAsync(ct);
    }

    public async Task<CreditCardInvoice?> GetByIdAndUserAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        return await DbSet
            .Include(i => i.CreditCardGroup)
            .ThenInclude(g => g.Cards)
            .Include(i => i.PaidFromAccount)
            .Include(i => i.Expenses)
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
    }

    public new async Task AddAsync(CreditCardInvoice invoice, CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(invoice, cancellationToken);
    }

    public new Task UpdateAsync(CreditCardInvoice invoice, CancellationToken cancellationToken = default)
    {
        DbSet.Update(invoice);
        return Task.CompletedTask;
    }

    public new Task DeleteAsync(CreditCardInvoice invoice, CancellationToken cancellationToken = default)
    {
        DbSet.Remove(invoice);
        return Task.CompletedTask;
    }
}
