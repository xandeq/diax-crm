using Diax.Domain.UserGroups;

namespace Diax.Domain.AI;

public class GroupAiProviderAccess
{
    public Guid GroupId { get; private set; }
    public Guid ProviderId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public UserGroup Group { get; private set; }
    public AiProvider Provider { get; private set; }

    public GroupAiProviderAccess(Guid groupId, Guid providerId)
    {
        GroupId = groupId;
        ProviderId = providerId;
        CreatedAt = DateTime.UtcNow;
    }
}
