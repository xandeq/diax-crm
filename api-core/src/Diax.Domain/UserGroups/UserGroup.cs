using Diax.Domain.Common;

namespace Diax.Domain.UserGroups;

public class UserGroup : AuditableEntity
{
    public string Key { get; private set; }
    public string Name { get; private set; }
    public bool IsSystem { get; private set; }
    public string? Description { get; private set; }

    private readonly List<UserGroupMember> _members = new();
    public IReadOnlyCollection<UserGroupMember> Members => _members.AsReadOnly();

    public UserGroup(string key, string name, bool isSystem, string? description = null)
    {
        Key = key;
        Name = name;
        IsSystem = isSystem;
        Description = description;
    }

    public void Update(string name, string? description)
    {
        Name = name;
        Description = description;
    }

    public void AddMember(Guid userId)
    {
        if (_members.Any(m => m.UserId == userId)) return;
        _members.Add(new UserGroupMember(userId, Id));
    }

    public void RemoveMember(Guid userId)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member != null) _members.Remove(member);
    }
}
