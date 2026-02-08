using Diax.Domain.Common;

namespace Diax.Domain.UserGroups;

public interface IUserGroupRepository : IRepository<UserGroup>
{
    Task<IEnumerable<UserGroup>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserGroup?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<UserGroup?> GetByIdWithMembersAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserGroup>> GetAllWithMembersAsync(CancellationToken cancellationToken = default);
}
