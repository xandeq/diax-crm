using Diax.Domain.Auth;

namespace Diax.Domain.UserGroups;

public class UserGroupMember
{
    public Guid UserId { get; private set; }
    public Guid GroupId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public User User { get; private set; } = null!;
    public UserGroup Group { get; private set; } = null!;

    // EF Core
    private UserGroupMember() { }

    public UserGroupMember(Guid userId, Guid groupId)
    {
        UserId = userId;
        GroupId = groupId;
        CreatedAt = DateTime.UtcNow;
    }
}
