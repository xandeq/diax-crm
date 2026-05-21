namespace Diax.Application.AiChat.Dtos;

// ===== REQUEST =====

/// <summary>Anexo enviado pelo client (lido client-side, conteúdo já em texto).</summary>
public record AttachmentDto(
    string FileName,
    string ContentType,
    int SizeBytes,
    string Content);

/// <summary>Payload do POST /chat/stream.</summary>
public record ChatRequestDto
{
    /// <summary>Id da conversa existente. Se null, será criada uma nova conversa nesta request.</summary>
    public Guid? ConversationId { get; init; }

    /// <summary>Modelo Anthropic (claude-sonnet-4-5, claude-haiku-4-5, claude-opus-4-7).</summary>
    public string Model { get; init; } = "claude-sonnet-4-5";

    /// <summary>System prompt opcional. Aplicado apenas ao criar conversa nova.</summary>
    public string? SystemPrompt { get; init; }

    /// <summary>Mensagem do usuário (texto puro).</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Arquivos anexados (no máximo 2MB total).</summary>
    public List<AttachmentDto> Attachments { get; init; } = new();
}

// ===== STREAMING =====

/// <summary>Tipos de chunk emitidos via SSE.</summary>
public static class ChatStreamChunkType
{
    public const string ConversationStarted = "conversation_started";
    public const string ContentDelta = "content_delta";
    public const string MessageStop = "message_stop";
    public const string Usage = "usage";
    public const string Title = "title";
    public const string Error = "error";
}

/// <summary>Chunk individual enviado via SSE para o frontend.</summary>
public record ChatStreamChunkDto(
    string Type,
    Guid? ConversationId = null,
    Guid? MessageId = null,
    string? Delta = null,
    string? Title = null,
    string? StopReason = null,
    UsageDto? Usage = null,
    string? Error = null);

public record UsageDto(
    int InputTokens,
    int OutputTokens,
    int CacheReadTokens,
    int CacheCreationTokens,
    decimal CostUsd);

// ===== CONVERSATION CRUD =====

public record ConversationListItemDto(
    Guid Id,
    string Title,
    string Model,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool IsArchived,
    int MessageCount);

public record ConversationDetailDto(
    Guid Id,
    string Title,
    string Model,
    string? SystemPrompt,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool IsArchived,
    List<MessageDto> Messages);

public record MessageDto(
    Guid Id,
    string Role,
    string Content,
    int InputTokens,
    int OutputTokens,
    int CacheReadTokens,
    int CacheCreationTokens,
    decimal CostUsd,
    DateTime CreatedAt,
    List<AttachmentMetaDto> Attachments);

public record AttachmentMetaDto(
    Guid Id,
    string FileName,
    string ContentType,
    int SizeBytes);

public record CreateConversationRequest(string? Title, string Model, string? SystemPrompt);

public record UpdateConversationRequest(string? Title, string? SystemPrompt, bool? IsArchived);

// ===== USAGE SUMMARY =====

public record MonthlyUsageDto(
    int Year,
    int Month,
    int InputTokens,
    int OutputTokens,
    int CacheReadTokens,
    int CacheCreationTokens,
    int TotalTokens,
    decimal CostUsd);
