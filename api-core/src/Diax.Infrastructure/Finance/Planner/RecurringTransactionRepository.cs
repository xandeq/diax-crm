using Diax.Domain.Finance.Planner;
using Diax.Domain.Finance.Planner.Repositories;
using Diax.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Finance.Planner;

public class RecurringTransactionRepository : Repository<RecurringTransaction>, IRecurringTransactionRepository
{
    public RecurringTransactionRepository(DiaxDbContext context) : base(context)
    {
    }

    public async Task<RecurringTransaction?> GetByIdAsync(Guid id, Guid userId)
    {
        return await Context.RecurringTransactions
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
    }

    public async Task<List<RecurringTransaction>> GetAllByUserIdAsync(Guid userId)
    {
        return await Context.RecurringTransactions
            .Where(r => r.UserId == userId)
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.DayOfMonth)
            .ToListAsync();
    }

    public async Task<List<RecurringTransaction>> GetActiveRecurringByUserIdAsync(Guid userId)
    {
        return await Context.RecurringTransactions
            .Where(r => r.UserId == userId && r.IsActive)
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.DayOfMonth)
            .ToListAsync();
    }

    public async Task<List<RecurringTransaction>> GetRecurringForMonthAsync(Guid userId, int month, int year)
    {
        var monthStart = new DateTime(year, month, 1);
        var monthEnd = new DateTime(year, month, DateTime.DaysInMonth(year, month));

        return await Context.RecurringTransactions
            .Where(r => r.UserId == userId
                && r.IsActive
                && r.StartDate <= monthEnd
                && (!r.EndDate.HasValue || r.EndDate.Value >= monthStart))
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.DayOfMonth)
            .ToListAsync();
    }

    public async Task<RecurringTransaction> AddAsync(RecurringTransaction recurring)
    {
        await Context.RecurringTransactions.AddAsync(recurring);
        return recurring;
    }

    public async Task UpdateAsync(RecurringTransaction recurring)
    {
        Context.RecurringTransactions.Update(recurring);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var recurring = await GetByIdAsync(id, userId);
        if (recurring != null)
        {
            Context.RecurringTransactions.Remove(recurring);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, Guid userId)
    {
        return await Context.RecurringTransactions
            .AnyAsync(r => r.Id == id && r.UserId == userId);
    }

    public async Task<bool> ExistsDuplicateAsync(Guid userId, string description, int dayOfMonth, decimal amount, Diax.Domain.Finance.TransactionType type, RecurringItemKind itemKind, Guid? excludeId = null)
    {
        int typeInt = (int)type;
        var plannerType = (Diax.Domain.Finance.Planner.TransactionType)typeInt;
        return await Context.RecurringTransactions
            .AnyAsync(r =>
                r.UserId == userId
                && r.Description == description
                && r.DayOfMonth == dayOfMonth
                && r.Amount == amount
                && r.Type == plannerType
                && r.ItemKind == itemKind
                && (!excludeId.HasValue || r.Id != excludeId.Value));
    }
}
