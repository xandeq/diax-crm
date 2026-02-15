namespace Diax.Domain.Finance.Planner.Repositories;

/// <summary>
/// Repositório para simulações mensais
/// </summary>
public interface IMonthlySimulationRepository
{
    Task<MonthlySimulation?> GetByIdAsync(Guid id, Guid userId);
    Task<MonthlySimulation?> GetByMonthYearAsync(Guid userId, int month, int year);
    Task<MonthlySimulation?> GetActiveSimulationForMonthAsync(Guid userId, int month, int year);
    Task<List<MonthlySimulation>> GetAllByUserIdAsync(Guid userId);
    Task<List<MonthlySimulation>> GetRecentSimulationsAsync(Guid userId, int count = 12);
    Task<MonthlySimulation> AddAsync(MonthlySimulation simulation);
    Task UpdateAsync(MonthlySimulation simulation);
    Task DeleteAsync(Guid id, Guid userId);
    Task ArchiveOldSimulationsAsync(Guid userId, DateTime olderThan);
}
