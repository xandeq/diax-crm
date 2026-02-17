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

    public async Task<List<ImportedTransaction>> GetByExpenseIdAsync(Guid expenseId, CancellationToken cancellationToken = default)
    {
        return await context.ImportedTransactions
            .Where(x => x.MatchedExpenseId == expenseId || x.CreatedExpenseId == expenseId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ImportedTransaction>> GetByIncomeIdAsync(Guid incomeId, CancellationToken cancellationToken = default)
    {
        return await context.ImportedTransactions
            .Where(x => x.CreatedIncomeId == incomeId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ImportedTransaction>> GetByTransactionIdAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        return await context.ImportedTransactions
            .Where(x => x.CreatedTransactionId == transactionId || x.MatchedTransactionId == transactionId)
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

    public async Task<IEnumerable<ImportedTransaction>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await context.ImportedTransactions
            .Where(x => x.UserId == userId)
            .ToListAsync(ct);
    }

    public async Task<ImportedTransaction?> GetByIdAndUserAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        return await context.ImportedTransactions
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
    }
}
