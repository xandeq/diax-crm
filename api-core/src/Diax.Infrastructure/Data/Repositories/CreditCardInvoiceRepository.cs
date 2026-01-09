using Diax.Domain.Finance;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class CreditCardInvoiceRepository : Repository<CreditCardInvoice>, ICreditCardInvoiceRepository
{
    public CreditCardInvoiceRepository(DiaxDbContext context) : base(context)
    {
    }

    public async Task<CreditCardInvoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(i => i.CreditCard)
            .Include(i => i.PaidFromAccount)
            .Include(i => i.Expenses)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<List<CreditCardInvoice>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(i => i.CreditCard)
            .OrderByDescending(i => i.ReferenceYear)
            .ThenByDescending(i => i.ReferenceMonth)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CreditCardInvoice>> GetByCreditCardIdAsync(Guid creditCardId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(i => i.CreditCardId == creditCardId)
            .Include(i => i.Expenses)
            .OrderByDescending(i => i.ReferenceYear)
            .ThenByDescending(i => i.ReferenceMonth)
            .ToListAsync(cancellationToken);
    }

    public async Task<CreditCardInvoice?> GetByCardAndPeriodAsync(Guid creditCardId, int month, int year, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(i => i.CreditCard)
            .Include(i => i.Expenses)
            .FirstOrDefaultAsync(i => i.CreditCardId == creditCardId 
                && i.ReferenceMonth == month 
                && i.ReferenceYear == year, cancellationToken);
    }

    public async Task<List<CreditCardInvoice>> GetUnpaidInvoicesAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(i => !i.IsPaid)
            .Include(i => i.CreditCard)
            .OrderBy(i => i.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(CreditCardInvoice invoice, CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(invoice, cancellationToken);
    }

    public Task UpdateAsync(CreditCardInvoice invoice, CancellationToken cancellationToken = default)
    {
        DbSet.Update(invoice);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(CreditCardInvoice invoice, CancellationToken cancellationToken = default)
    {
        DbSet.Remove(invoice);
        return Task.CompletedTask;
    }
}
