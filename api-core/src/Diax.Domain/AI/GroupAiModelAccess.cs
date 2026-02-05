using Diax.Domain.UserGroups;

namespace Diax.Domain.AI;

public class GroupAiModelAccess
{
    public Guid GroupId { get; private set; }
    public Guid AiModelId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public UserGroup Group { get; private set; }
    public AiModel Model { get; private set; }

    public GroupAiModelAccess(Guid groupId, Guid aiModelId)
    {
        GroupId = groupId;
        AiModelId = aiModelId;
        CreatedAt = DateTime.UtcNow;
    }
}
