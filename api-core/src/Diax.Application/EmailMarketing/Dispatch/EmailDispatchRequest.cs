namespace Diax.Application.EmailMarketing.Dispatch;

public sealed record EmailDispatchRequest(
    EmailMarketing.EmailMessage Message,
    string? IdempotencyKey,
    string? ProviderHint,
    string RequestId,
    bool AllowUnaligned   // false = nunca usa Tier 2; true = tenta Tier 2 se Tier 1 esgotar
);
