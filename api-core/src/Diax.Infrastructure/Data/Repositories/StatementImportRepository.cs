using Diax.Domain.Finance;
using Diax.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class StatementImportRepository(DiaxDbContext context) : IStatementImportRepository
{
    public async Task<StatementImport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.StatementImports
            .Include(x => x.Transactions)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<List<StatementImport>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await context.StatementImports
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(StatementImport import, CancellationToken cancellationToken = default)
    {
        await context.StatementImports.AddAsync(import, cancellationToken);
    }

    public async Task UpdateAsync(StatementImport import, CancellationToken cancellationToken = default)
    {
        context.StatementImports.Update(import);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(StatementImport import, CancellationToken cancellationToken = default)
    {
        context.StatementImports.Remove(import);
        await Task.CompletedTask;
    }
}
