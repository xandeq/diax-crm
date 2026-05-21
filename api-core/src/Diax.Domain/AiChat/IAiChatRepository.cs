using Diax.Domain.Common;
using Diax.Shared.Results;

namespace Diax.Domain.AiChat;

public interface IAiChatRepository : IRepository<AiConversation>
{
    /// <summary>Lista paginada de conversas (não arquivadas) do usuário, mais recentes primeiro.</summary>
    Task<PagedResult<AiConversation>> GetPagedByUserAsync(
        Guid userId,
        int page,
        int pageSize,
        bool includeArchived = false,
        CancellationToken cancellationToken = default);

    /// <summary>Carrega conversa com todas as mensagens e anexos eager-loaded.</summary>
    Task<AiConversation?> GetWithMessagesAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>Apenas a conversa, sem mensagens — útil para mutations leves (rename, archive).</summary>
    Task<AiConversation?> GetByIdForUserAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persiste uma mensagem diretamente no DbSet, sem passar pela navigation property da conversa.
    /// Isso evita que EF Core marque AiConversation como Modified (evita UPDATE desnecessário
    /// que pode resultar em DbUpdateConcurrencyException no path de streaming).
    /// EF Core relationship fix-up automaticamente adiciona a mensagem ao conv._messages.
    /// </summary>
    Task AddMessageAsync(AiChatMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove uma mensagem diretamente do DbSet (sem passar pela navigation property da conversa).
    /// Usado para apagar o placeholder do assistente quando o cliente cancela antes de receber resposta.
    /// </summary>
    Task DeleteMessageAsync(AiChatMessage message, CancellationToken cancellationToken = default);

    /// <summary>Somatório de tokens e custo USD do usuário no mês corrente (UTC).</summary>
    Task<(int InputTokens, int OutputTokens, int CacheReadTokens, int CacheCreationTokens, decimal CostUsd)>
        GetMonthlyUsageAsync(
            Guid userId,
            DateTime monthStartUtc,
            CancellationToken cancellationToken = default);
}
