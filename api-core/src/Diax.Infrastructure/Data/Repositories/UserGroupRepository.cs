using Diax.Domain.UserGroups;
using Diax.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class UserGroupRepository : Repository<UserGroup>, IUserGroupRepository
{
    public UserGroupRepository(DiaxDbContext context) : base(context) { }

    public async Task<IEnumerable<UserGroup>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await Context.UserGroupMembers
            .Where(x => x.UserId == userId)
            .Select(x => x.Group)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserGroup?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(x => x.Key == key, cancellationToken);
    }

    public async Task<UserGroup?> GetByIdWithMembersAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
             .Include(g => g.Members)
                .ThenInclude(m => m.User)
             .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<UserGroup>> GetAllWithMembersAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(g => g.Members)
            .OrderBy(g => g.Name)
            .ToListAsync(cancellationToken);
    }
}
