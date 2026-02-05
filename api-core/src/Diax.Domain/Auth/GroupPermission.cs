using Diax.Domain.UserGroups;

namespace Diax.Domain.Auth;

public class GroupPermission
{
    public Guid GroupId { get; private set; }
    public Guid PermissionId { get; private set; }

    public UserGroup Group { get; private set; }
    public Permission Permission { get; private set; }

    public GroupPermission(Guid groupId, Guid permissionId)
    {
        GroupId = groupId;
        PermissionId = permissionId;
    }
}
