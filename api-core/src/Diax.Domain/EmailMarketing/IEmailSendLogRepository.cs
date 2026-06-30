namespace Diax.Domain.EmailMarketing;

public interface IEmailSendLogRepository
{
    /// <summary>
    /// Busca o registro mais recente com essa chave dentro da janela de tempo (para dedup).
    /// </summary>
    Task<EmailSendLog?> FindRecentByIdempotencyKeyAsync(
        string idempotencyKey,
        TimeSpan window,
        CancellationToken ct = default);

    /// <summary>
    /// Cria e salva um registro InFlight (ANTES de tentar os providers).
    /// </summary>
    Task<EmailSendLog> CreateInFlightAsync(
        string requestId,
        string? idempotencyKey,
        string toHash,
        string subjectHash,
        string? bodyHash,
        string fromDomain,
        CancellationToken ct = default);

    /// <summary>
    /// Persiste uma tentativa (pode ser chamado várias vezes para o mesmo requestId).
    /// </summary>
    Task RecordAttemptAsync(
        Guid logId,
        string provider,
        int attemptNo,
        bool success,
        string? providerMessageId,
        string? error,
        int latencyMs,
        bool allowUnaligned,
        CancellationToken ct = default);

    /// <summary>
    /// Marca o registro como Sent.
    /// </summary>
    Task MarkSentAsync(
        Guid logId,
        string provider,
        string? providerMessageId,
        bool allowUnaligned,
        CancellationToken ct = default);

    /// <summary>
    /// Marca o registro como Failed (todos os providers esgotados).
    /// </summary>
    Task MarkFailedAsync(Guid logId, CancellationToken ct = default);
}
