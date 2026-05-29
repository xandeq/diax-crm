using Diax.Domain.Agents;
using Diax.Domain.Common;

namespace Diax.Domain.AiChat;

/// <summary>
/// Uma conversa (thread) com a IA. Conversa = container de mensagens trocadas
/// entre o usuário e o modelo selecionado. Pertence a um usuário (multi-tenancy filtrado).
/// </summary>
public class AiConversation : AuditableEntity, IUserOwnedEntity
{
    private readonly List<AiChatMessage> _messages = new();

    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public string? SystemPrompt { get; private set; }
    public bool IsArchived { get; private set; }

    /// <summary>
    /// Null = regular AiChat conversation (existing behavior unchanged).
    /// Non-null = agent conversation of the given type.
    /// Set once via SetAgentType(); existing AiChat conversations remain null.
    /// </summary>
    public AgentType? AgentType { get; private set; }

    public IReadOnlyCollection<AiChatMessage> Messages => _messages.AsReadOnly();

    // EF Core
    private AiConversation() { }

    public AiConversation(
        Guid userId,
        string title,
        string model,
        string? systemPrompt = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));

        UserId = userId;
        SetTitle(title);
        SetModel(model);
        SetSystemPrompt(systemPrompt);
    }

    public void SetTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        Title = title.Trim().Length > 200 ? title.Trim()[..200] : title.Trim();
        SetUpdated();
    }

    public void SetModel(string model)
    {
        if (string.IsNullOrWhiteSpace(model))
            throw new ArgumentException("Model is required.", nameof(model));

        Model = model.Trim();
        SetUpdated();
    }

    public void SetSystemPrompt(string? systemPrompt)
    {
        SystemPrompt = string.IsNullOrWhiteSpace(systemPrompt) ? null : systemPrompt;
        SetUpdated();
    }

    public void Archive()
    {
        IsArchived = true;
        SetUpdated();
    }

    public void Unarchive()
    {
        IsArchived = false;
        SetUpdated();
    }

    /// <summary>
    /// Associates this conversation with a specific agent type.
    /// Should be called once when creating an agent conversation.
    /// </summary>
    public void SetAgentType(AgentType agentType)
    {
        AgentType = agentType;
        SetUpdated();
    }

    public AiChatMessage AddMessage(string role, string content)
    {
        var message = new AiChatMessage(Id, role, content);
        _messages.Add(message);
        SetUpdated();
        return message;
    }

    public AiChatMessage AddAssistantMessage(
        string content,
        int inputTokens,
        int outputTokens,
        int cacheReadTokens,
        int cacheCreationTokens,
        decimal costUsd)
    {
        var message = new AiChatMessage(
            Id,
            "assistant",
            content,
            inputTokens,
            outputTokens,
            cacheReadTokens,
            cacheCreationTokens,
            costUsd);
        _messages.Add(message);
        SetUpdated();
        return message;
    }
}
