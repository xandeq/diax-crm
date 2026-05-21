using Diax.Domain.Common;

namespace Diax.Domain.AiChat;

/// <summary>
/// Uma mensagem (turn) dentro de uma conversa AI. Pode ser do usuário ou do assistente.
/// Armazena tokens e custo apenas para mensagens de assistente.
/// </summary>
public class AiChatMessage : AuditableEntity
{
    private readonly List<AiChatAttachment> _attachments = new();

    public Guid ConversationId { get; private set; }
    public string Role { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public int InputTokens { get; private set; }
    public int OutputTokens { get; private set; }
    public int CacheReadTokens { get; private set; }
    public int CacheCreationTokens { get; private set; }
    public decimal CostUsd { get; private set; }

    public IReadOnlyCollection<AiChatAttachment> Attachments => _attachments.AsReadOnly();

    // EF Core
    private AiChatMessage() { }

    public AiChatMessage(Guid conversationId, string role, string content)
    {
        if (conversationId == Guid.Empty)
            throw new ArgumentException("ConversationId is required.", nameof(conversationId));

        SetRole(role);
        SetContent(content);
        ConversationId = conversationId;
    }

    public AiChatMessage(
        Guid conversationId,
        string role,
        string content,
        int inputTokens,
        int outputTokens,
        int cacheReadTokens,
        int cacheCreationTokens,
        decimal costUsd) : this(conversationId, role, content)
    {
        InputTokens = inputTokens;
        OutputTokens = outputTokens;
        CacheReadTokens = cacheReadTokens;
        CacheCreationTokens = cacheCreationTokens;
        CostUsd = costUsd;
    }

    public void SetRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("Role is required.", nameof(role));

        var normalized = role.Trim().ToLowerInvariant();
        if (normalized != "user" && normalized != "assistant")
            throw new ArgumentException("Role must be 'user' or 'assistant'.", nameof(role));

        Role = normalized;
    }

    public void SetContent(string content)
    {
        // Permite content vazio (ex: streaming abortado antes de chegar texto)
        Content = content ?? string.Empty;
    }

    public void AppendContent(string chunk)
    {
        if (!string.IsNullOrEmpty(chunk))
            Content += chunk;
    }

    public void UpdateUsage(
        int inputTokens,
        int outputTokens,
        int cacheReadTokens,
        int cacheCreationTokens,
        decimal costUsd)
    {
        InputTokens = inputTokens;
        OutputTokens = outputTokens;
        CacheReadTokens = cacheReadTokens;
        CacheCreationTokens = cacheCreationTokens;
        CostUsd = costUsd;
        SetUpdated();
    }

    public void AttachFile(string fileName, string contentType, int sizeBytes, string content)
    {
        var attachment = new AiChatAttachment(Id, fileName, contentType, sizeBytes, content);
        _attachments.Add(attachment);
    }
}
