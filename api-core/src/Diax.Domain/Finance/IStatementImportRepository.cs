namespace Diax.Domain.Finance;

public interface IStatementImportRepository
{
    Task<StatementImport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<StatementImport>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(StatementImport import, CancellationToken cancellationToken = default);
    Task UpdateAsync(StatementImport import, CancellationToken cancellationToken = default);
    Task DeleteAsync(StatementImport import, CancellationToken cancellationToken = default);
}
