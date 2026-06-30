namespace Diax.Application.EmailMarketing.Dispatch;

public sealed record EmailDispatchResult(
    bool Success,
    EmailDispatchStatus Status,
    string? MessageId,
    string? ProviderUsed,
    bool AllowUnaligned,
    IReadOnlyList<EmailAttemptDetail> Attempts
);

public sealed record EmailAttemptDetail(
    string Provider,
    int AttemptNo,
    bool Success,
    string? ProviderMessageId,
    string? Error,
    long LatencyMs
);

public enum EmailDispatchStatus
{
    Sent,
    Duplicate,    // Idempotency replay — já enviado
    InProgress,   // Idempotency: InFlight em outra chamada
    AllFailed,    // Todos os providers falharam
    Rejected      // Sender domain não permitido / validação falhou
}
