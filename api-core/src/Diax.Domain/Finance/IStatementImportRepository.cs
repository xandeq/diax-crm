namespace Diax.Domain.Finance;

public interface IStatementImportRepository
{
    Task<StatementImport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<StatementImport>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(StatementImport import, CancellationToken cancellationToken = default);
    Task UpdateAsync(StatementImport import, CancellationToken cancellationToken = default);
    Task DeleteAsync(StatementImport import, CancellationToken cancellationToken = default);
    Task<IEnumerable<StatementImport>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<StatementImport?> GetByIdAndUserAsync(Guid id, Guid userId, CancellationToken ct = default);
}
