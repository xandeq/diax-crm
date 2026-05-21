using Diax.Application.AiChat.Dtos;
using Diax.Shared.Results;

namespace Diax.Application.AiChat;

public interface IAiChatService
{
    Task<Result<PagedResult<ConversationListItemDto>>> ListConversationsAsync(
        Guid userId,
        int page,
        int pageSize,
        bool includeArchived,
        CancellationToken cancellationToken = default);

    Task<Result<ConversationDetailDto>> GetConversationAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Result<ConversationDetailDto>> CreateConversationAsync(
        Guid userId,
        CreateConversationRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<ConversationDetailDto>> UpdateConversationAsync(
        Guid conversationId,
        Guid userId,
        UpdateConversationRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> ArchiveConversationAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stream principal — orquestra Anthropic + persistência. Yield de chunks SSE-ready.
    /// </summary>
    IAsyncEnumerable<ChatStreamChunkDto> StreamChatAsync(
        Guid userId,
        ChatRequestDto request,
        CancellationToken cancellationToken = default);

    Task<Result<MonthlyUsageDto>> GetMonthlyUsageAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
