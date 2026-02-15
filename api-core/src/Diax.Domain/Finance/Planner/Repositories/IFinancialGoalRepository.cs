namespace Diax.Domain.Finance.Planner.Repositories;

/// <summary>
/// Repositório para metas financeiras
/// </summary>
public interface IFinancialGoalRepository
{
    Task<FinancialGoal?> GetByIdAsync(Guid id, Guid userId);
    Task<List<FinancialGoal>> GetAllByUserIdAsync(Guid userId);
    Task<List<FinancialGoal>> GetActiveGoalsByUserIdAsync(Guid userId);
    Task<List<FinancialGoal>> GetGoalsWithAutoAllocateAsync(Guid userId);
    Task<FinancialGoal> AddAsync(FinancialGoal goal);
    Task UpdateAsync(FinancialGoal goal);
    Task DeleteAsync(Guid id, Guid userId);
    Task<bool> ExistsAsync(Guid id, Guid userId);
}
