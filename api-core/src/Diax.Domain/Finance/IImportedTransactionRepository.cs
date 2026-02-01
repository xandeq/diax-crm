namespace Diax.Domain.Finance;

public interface IImportedTransactionRepository
{
    Task<ImportedTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<ImportedTransaction>> GetByImportIdAsync(Guid importId, CancellationToken cancellationToken = default);
    Task<List<ImportedTransaction>> GetByExpenseIdAsync(Guid expenseId, CancellationToken cancellationToken = default);
    Task<List<ImportedTransaction>> GetByIncomeIdAsync(Guid incomeId, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<ImportedTransaction> transactions, CancellationToken cancellationToken = default);
    Task UpdateAsync(ImportedTransaction transaction, CancellationToken cancellationToken = default);
}
