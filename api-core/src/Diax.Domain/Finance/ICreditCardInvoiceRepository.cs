namespace Diax.Domain.Finance;

public interface ICreditCardInvoiceRepository
{
    Task<CreditCardInvoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<CreditCardInvoice>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<CreditCardInvoice>> GetByCreditCardIdAsync(Guid creditCardId, CancellationToken cancellationToken = default);
    Task<List<CreditCardInvoice>> GetByGroupIdAsync(Guid creditCardGroupId, CancellationToken cancellationToken = default);
    Task<CreditCardInvoice?> GetByCardAndPeriodAsync(Guid creditCardId, int month, int year, CancellationToken cancellationToken = default);
    Task<CreditCardInvoice?> GetByGroupAndPeriodAsync(Guid creditCardGroupId, int month, int year, CancellationToken cancellationToken = default);
    Task<List<CreditCardInvoice>> GetUnpaidInvoicesAsync(CancellationToken cancellationToken = default);
    Task AddAsync(CreditCardInvoice invoice, CancellationToken cancellationToken = default);
    Task UpdateAsync(CreditCardInvoice invoice, CancellationToken cancellationToken = default);
    Task DeleteAsync(CreditCardInvoice invoice, CancellationToken cancellationToken = default);
    Task<IEnumerable<CreditCardInvoice>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<List<CreditCardInvoice>> GetByReferencePeriodAsync(Guid userId, int month, int year, CancellationToken ct = default);
    Task<List<CreditCardInvoice>> GetByDueDateRangeAsync(Guid userId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<CreditCardInvoice?> GetByIdAndUserAsync(Guid id, Guid userId, CancellationToken ct = default);
}
