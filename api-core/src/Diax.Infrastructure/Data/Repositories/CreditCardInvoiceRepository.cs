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
        // GetTotalAmount() reads BOTH .Transactions (post-unification fd9eee0) and
        // .Expenses (legacy). Loading only one yields a wrong total during the
        // transition window — see commit 6c60317 for the entity-level fix.
        return await DbSet
            .Include(i => i.CreditCardGroup)
            .ThenInclude(g => g.Cards)
            .Include(i => i.PaidFromAccount)
            .Include(i => i.Transactions)
#pragma warning disable CS0618
            .Include(i => i.Expenses)
#pragma warning restore CS0618
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
            .Include(i => i.Transactions)
#pragma warning disable CS0618
            .Include(i => i.Expenses)
#pragma warning restore CS0618
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
            .Include(i => i.Transactions)
#pragma warning disable CS0618
            .Include(i => i.Expenses)
#pragma warning restore CS0618
            .FirstOrDefaultAsync(i => i.CreditCardGroupId == card.CreditCardGroupId
                && i.ReferenceMonth == month
                && i.ReferenceYear == year, cancellationToken);
    }

    public async Task<List<CreditCardInvoice>> GetByGroupIdAsync(Guid creditCardGroupId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(i => i.CreditCardGroupId == creditCardGroupId)
            .Include(i => i.Transactions)
#pragma warning disable CS0618
            .Include(i => i.Expenses)
#pragma warning restore CS0618
            .OrderByDescending(i => i.ReferenceYear)
            .ThenByDescending(i => i.ReferenceMonth)
            .ToListAsync(cancellationToken);
    }

    public async Task<CreditCardInvoice?> GetByGroupAndPeriodAsync(Guid creditCardGroupId, int month, int year, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(i => i.CreditCardGroup)
            .ThenInclude(g => g.Cards)
            .Include(i => i.Transactions)
#pragma warning disable CS0618
            .Include(i => i.Expenses)
#pragma warning restore CS0618
            .FirstOrDefaultAsync(i => i.CreditCardGroupId == creditCardGroupId
                && i.ReferenceMonth == month
                && i.ReferenceYear == year, cancellationToken);
    }

    public async Task<List<CreditCardInvoice>> GetUnpaidInvoicesAsync(CancellationToken cancellationToken = default)
    {
        // FinancialSummaryService.cs:90 sums GetTotalAmount() over the result, so we must
        // load both Transactions (post-unification) and the legacy Expenses navigation.
        return await DbSet
            .Where(i => !i.IsPaid)
            .Include(i => i.CreditCardGroup)
            .ThenInclude(g => g.Cards)
            .Include(i => i.Transactions)
#pragma warning disable CS0618
            .Include(i => i.Expenses)
#pragma warning restore CS0618
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

    public async Task<List<CreditCardInvoice>> GetByReferencePeriodAsync(Guid userId, int month, int year, CancellationToken ct = default)
    {
        return await DbSet
            .Include(i => i.CreditCardGroup)
            .Where(i => i.UserId == userId && i.ReferenceMonth == month && i.ReferenceYear == year)
            .OrderBy(i => i.DueDate)
            .ToListAsync(ct);
    }

    public async Task<List<CreditCardInvoice>> GetByDueDateRangeAsync(Guid userId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        return await DbSet
            .Include(i => i.CreditCardGroup)
            .Where(i => i.UserId == userId && i.DueDate >= from && i.DueDate <= to)
            .OrderBy(i => i.DueDate)
            .ToListAsync(ct);
    }

    public async Task<CreditCardInvoice?> GetByIdAndUserAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        return await DbSet
            .Include(i => i.CreditCardGroup)
            .ThenInclude(g => g.Cards)
            .Include(i => i.PaidFromAccount)
            .Include(i => i.Transactions)
#pragma warning disable CS0618
            .Include(i => i.Expenses)
#pragma warning restore CS0618
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
