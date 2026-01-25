using Diax.Domain.Finance;
using Diax.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class ImportedTransactionRepository(DiaxDbContext context) : IImportedTransactionRepository
{
    public async Task<ImportedTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.ImportedTransactions
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<List<ImportedTransaction>> GetByImportIdAsync(Guid importId, CancellationToken cancellationToken = default)
    {
        return await context.ImportedTransactions
            .Where(x => x.StatementImportId == importId)
            .OrderBy(x => x.TransactionDate)
            .ToListAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<ImportedTransaction> transactions, CancellationToken cancellationToken = default)
    {
        await context.ImportedTransactions.AddRangeAsync(transactions, cancellationToken);
    }

    public async Task UpdateAsync(ImportedTransaction transaction, CancellationToken cancellationToken = default)
    {
        context.ImportedTransactions.Update(transaction);
        await Task.CompletedTask;
    }
}
