using Diax.Domain.Finance.Planner;
using Diax.Domain.Finance.Planner.Repositories;
using Diax.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Finance.Planner;

public class FinancialGoalRepository : Repository<FinancialGoal>, IFinancialGoalRepository
{
    public FinancialGoalRepository(DiaxDbContext context) : base(context)
    {
    }

    public async Task<FinancialGoal?> GetByIdAsync(Guid id, Guid userId)
    {
        return await Context.FinancialGoals
            .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);
    }

    public async Task<List<FinancialGoal>> GetAllByUserIdAsync(Guid userId)
    {
        return await Context.FinancialGoals
            .Where(g => g.UserId == userId)
            .OrderBy(g => g.Priority)
            .ThenByDescending(g => g.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<FinancialGoal>> GetActiveGoalsByUserIdAsync(Guid userId)
    {
        return await Context.FinancialGoals
            .Where(g => g.UserId == userId && g.IsActive)
            .OrderBy(g => g.Priority)
            .ToListAsync();
    }

    public async Task<List<FinancialGoal>> GetGoalsWithAutoAllocateAsync(Guid userId)
    {
        return await Context.FinancialGoals
            .Where(g => g.UserId == userId && g.IsActive && g.AutoAllocateSurplus)
            .OrderBy(g => g.Priority)
            .ToListAsync();
    }

    public async Task<FinancialGoal> AddAsync(FinancialGoal goal)
    {
        await Context.FinancialGoals.AddAsync(goal);
        return goal;
    }

    public async Task UpdateAsync(FinancialGoal goal)
    {
        Context.FinancialGoals.Update(goal);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var goal = await GetByIdAsync(id, userId);
        if (goal != null)
        {
            Context.FinancialGoals.Remove(goal);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, Guid userId)
    {
        return await Context.FinancialGoals
            .AnyAsync(g => g.Id == id && g.UserId == userId);
    }
}
