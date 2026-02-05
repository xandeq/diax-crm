using Diax.Domain.UserGroups;

namespace Diax.Application.Auth;

public interface IUserGroupService
{
    Task<List<UserGroup>> GetAllAsync();
    Task<UserGroup?> GetByIdAsync(Guid id);
    Task<UserGroup> CreateAsync(string name, string description);
    Task UpdateAsync(Guid id, string name, string description);
    Task DeleteAsync(Guid id);

    // Member management
    Task AddMemberAsync(Guid groupId, Guid userId);
    Task RemoveMemberAsync(Guid groupId, Guid userId);
}
