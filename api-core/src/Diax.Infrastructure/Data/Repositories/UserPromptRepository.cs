using Diax.Domain.PromptGenerator;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class UserPromptRepository : Repository<UserPrompt>, IUserPromptRepository
{
    public UserPromptRepository(DiaxDbContext context) : base(context) { }

    public async Task<IEnumerable<UserPrompt>> GetByUserIdAsync(
        Guid userId,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt);

        if (limit.HasValue)
            return await query.Take(limit.Value).ToListAsync(cancellationToken);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<UserPrompt?> GetByIdWithUserAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId, cancellationToken);
    }
}
