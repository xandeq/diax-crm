using Diax.Domain.Finance;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class TransactionCategoryRepository : Repository<TransactionCategory>, ITransactionCategoryRepository
{
    public TransactionCategoryRepository(DiaxDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<TransactionCategory>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await DbSet
            .IgnoreQueryFilters()
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);
    }

    public async Task<TransactionCategory?> GetByIdAndUserAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        return await DbSet
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, ct);
    }

    public async Task<IEnumerable<TransactionCategory>> GetActiveAsync(Guid userId, CancellationToken ct = default)
    {
        return await DbSet
            .IgnoreQueryFilters()
            .Where(c => c.UserId == userId && c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<TransactionCategory>> GetByApplicableToAsync(CategoryApplicableTo applicableTo, Guid userId, CancellationToken ct = default)
    {
        return await DbSet
            .IgnoreQueryFilters()
            .Where(c => c.UserId == userId && c.IsActive &&
                        (c.ApplicableTo == applicableTo || c.ApplicableTo == CategoryApplicableTo.Both))
            .OrderBy(c => c.Name)
            .ToListAsync(ct);
    }
}
