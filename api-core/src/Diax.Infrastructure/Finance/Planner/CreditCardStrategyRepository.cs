using Diax.Domain.Finance.Planner;
using Diax.Domain.Finance.Planner.Repositories;
using Diax.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Finance.Planner;

public class CreditCardStrategyRepository : Repository<CreditCardStrategy>, ICreditCardStrategyRepository
{
    public CreditCardStrategyRepository(DiaxDbContext context) : base(context)
    {
    }

    public async Task<CreditCardStrategy?> GetByIdAsync(Guid id, Guid userId)
    {
        return await Context.CreditCardStrategies
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);
    }

    public async Task<CreditCardStrategy?> GetByCreditCardIdAsync(Guid creditCardId, Guid userId)
    {
        return await Context.CreditCardStrategies
            .FirstOrDefaultAsync(s => s.CreditCardId == creditCardId && s.UserId == userId);
    }

    public async Task<List<CreditCardStrategy>> GetAllByUserIdAsync(Guid userId)
    {
        return await Context.CreditCardStrategies
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.IsRecommended)
            .ThenByDescending(s => s.AvailableLimitPercentage)
            .ToListAsync();
    }

    public async Task<List<CreditCardStrategy>> GetRecommendedStrategiesAsync(Guid userId)
    {
        return await Context.CreditCardStrategies
            .Where(s => s.UserId == userId && s.IsRecommended)
            .OrderByDescending(s => s.AvailableLimitPercentage)
            .ThenBy(s => s.OptimalPurchaseDay)
            .ToListAsync();
    }

    public async Task<CreditCardStrategy> AddAsync(CreditCardStrategy strategy)
    {
        await Context.CreditCardStrategies.AddAsync(strategy);
        return strategy;
    }

    public async Task UpdateAsync(CreditCardStrategy strategy)
    {
        Context.CreditCardStrategies.Update(strategy);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var strategy = await GetByIdAsync(id, userId);
        if (strategy != null)
        {
            Context.CreditCardStrategies.Remove(strategy);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, Guid userId)
    {
        return await Context.CreditCardStrategies
            .AnyAsync(s => s.Id == id && s.UserId == userId);
    }
}
