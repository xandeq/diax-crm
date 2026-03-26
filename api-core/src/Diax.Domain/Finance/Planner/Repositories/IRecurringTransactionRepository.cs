namespace Diax.Domain.Finance.Planner.Repositories;

/// <summary>
/// Repositório para transações recorrentes
/// </summary>
public interface IRecurringTransactionRepository
{
    Task<RecurringTransaction?> GetByIdAsync(Guid id, Guid userId);
    Task<List<RecurringTransaction>> GetAllByUserIdAsync(Guid userId);
    Task<List<RecurringTransaction>> GetActiveRecurringByUserIdAsync(Guid userId);
    Task<List<RecurringTransaction>> GetRecurringForMonthAsync(Guid userId, int month, int year);
    Task<RecurringTransaction> AddAsync(RecurringTransaction recurring);
    Task UpdateAsync(RecurringTransaction recurring);
    Task DeleteAsync(Guid id, Guid userId);
    Task<bool> ExistsAsync(Guid id, Guid userId);
    Task<bool> ExistsDuplicateAsync(Guid userId, string description, int dayOfMonth, decimal amount, Diax.Domain.Finance.TransactionType type, RecurringItemKind itemKind, Guid? excludeId = null);
}
