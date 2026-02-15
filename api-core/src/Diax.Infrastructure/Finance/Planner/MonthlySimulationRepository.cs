using Diax.Domain.Finance.Planner;
using Diax.Domain.Finance.Planner.Repositories;
using Diax.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Finance.Planner;

public class MonthlySimulationRepository : Repository<MonthlySimulation>, IMonthlySimulationRepository
{
    public MonthlySimulationRepository(DiaxDbContext context) : base(context)
    {
    }

    public async Task<MonthlySimulation?> GetByIdAsync(Guid id, Guid userId)
    {
        return await Context.MonthlySimulations
            .Include(s => s.ProjectedTransactions.OrderBy(t => t.Date))
            .Include(s => s.DailyBalances.OrderBy(d => d.Date))
            .Include(s => s.Recommendations.OrderBy(r => r.Priority))
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);
    }

    public async Task<MonthlySimulation?> GetByMonthYearAsync(Guid userId, int month, int year)
    {
        return await Context.MonthlySimulations
            .Include(s => s.ProjectedTransactions.OrderBy(t => t.Date))
            .Include(s => s.DailyBalances.OrderBy(d => d.Date))
            .Include(s => s.Recommendations.OrderBy(r => r.Priority))
            .FirstOrDefaultAsync(s => s.UserId == userId
                && s.ReferenceMonth == month
                && s.ReferenceYear == year);
    }

    public async Task<MonthlySimulation?> GetActiveSimulationForMonthAsync(Guid userId, int month, int year)
    {
        return await Context.MonthlySimulations
            .Include(s => s.ProjectedTransactions.OrderBy(t => t.Date))
            .Include(s => s.DailyBalances.OrderBy(d => d.Date))
            .Include(s => s.Recommendations.OrderBy(r => r.Priority))
            .FirstOrDefaultAsync(s => s.UserId == userId
                && s.ReferenceMonth == month
                && s.ReferenceYear == year
                && s.Status == SimulationStatus.Active);
    }

    public async Task<List<MonthlySimulation>> GetAllByUserIdAsync(Guid userId)
    {
        return await Context.MonthlySimulations
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.ReferenceYear)
            .ThenByDescending(s => s.ReferenceMonth)
            .ToListAsync();
    }

    public async Task<List<MonthlySimulation>> GetRecentSimulationsAsync(Guid userId, int count = 12)
    {
        return await Context.MonthlySimulations
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.ReferenceYear)
            .ThenByDescending(s => s.ReferenceMonth)
            .Take(count)
            .ToListAsync();
    }

    public async Task<MonthlySimulation> AddAsync(MonthlySimulation simulation)
    {
        await Context.MonthlySimulations.AddAsync(simulation);
        return simulation;
    }

    public async Task UpdateAsync(MonthlySimulation simulation)
    {
        Context.MonthlySimulations.Update(simulation);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var simulation = await Context.MonthlySimulations
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

        if (simulation != null)
        {
            Context.MonthlySimulations.Remove(simulation);
        }
    }

    public async Task ArchiveOldSimulationsAsync(Guid userId, DateTime olderThan)
    {
        var oldSimulations = await Context.MonthlySimulations
            .Where(s => s.UserId == userId
                && s.SimulationDate < olderThan
                && s.Status == SimulationStatus.Active)
            .ToListAsync();

        foreach (var simulation in oldSimulations)
        {
            simulation.Status = SimulationStatus.Archived;
        }
    }
}
