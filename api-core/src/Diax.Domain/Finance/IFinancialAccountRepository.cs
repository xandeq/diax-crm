namespace Diax.Domain.Finance;

public interface IFinancialAccountRepository
{
    Task<FinancialAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<FinancialAccount>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<FinancialAccount>> GetActiveAccountsAsync(CancellationToken cancellationToken = default);
    Task AddAsync(FinancialAccount account, CancellationToken cancellationToken = default);
    Task UpdateAsync(FinancialAccount account, CancellationToken cancellationToken = default);
    Task DeleteAsync(FinancialAccount account, CancellationToken cancellationToken = default);
    Task<IEnumerable<FinancialAccount>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<FinancialAccount?> GetByIdAndUserAsync(Guid id, Guid userId, CancellationToken ct = default);
}
