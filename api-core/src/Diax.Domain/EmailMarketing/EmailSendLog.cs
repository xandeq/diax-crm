using Diax.Domain.Common;

namespace Diax.Domain.EmailMarketing;

/// <summary>
/// Log de auditoria de envios realizados pelo endpoint unificado de email.
/// Não implementa IUserOwnedEntity — é log de serviço, sem filtro por UserId.
/// </summary>
public class EmailSendLog : AuditableEntity
{
    /// <summary>Correlação por dispatch call.</summary>
    public string RequestId { get; private set; } = string.Empty;

    /// <summary>Chave de idempotência fornecida pelo caller (nullable).</summary>
    public string? IdempotencyKey { get; private set; }

    /// <summary>SHA256 dos destinatários normalizados — PII-safe.</summary>
    public string ToHash { get; private set; } = string.Empty;

    /// <summary>SHA256 do subject.</summary>
    public string SubjectHash { get; private set; } = string.Empty;

    /// <summary>SHA256 do html body (nullable).</summary>
    public string? BodyHash { get; private set; }

    /// <summary>Chave DI do provider usado, ex: "resend".</summary>
    public string Provider { get; private set; } = string.Empty;

    /// <summary>Número da tentativa (1-based).</summary>
    public int AttemptNo { get; private set; }

    /// <summary>"InFlight" | "Sent" | "Failed" | "Uncertain" | "Duplicate".</summary>
    public string Status { get; private set; } = string.Empty;

    /// <summary>Mensagem de erro (nullable).</summary>
    public string? Error { get; private set; }

    /// <summary>Duração da chamada ao provider em ms.</summary>
    public int LatencyMs { get; private set; }

    /// <summary>ID retornado pelo provider (nullable).</summary>
    public string? ProviderMessageId { get; private set; }

    /// <summary>Domínio do remetente para analytics.</summary>
    public string FromDomain { get; private set; } = string.Empty;

    /// <summary>
    /// True somente quando o provider usado é Tier 2 (SMTP próprio)
    /// por escolha explícita do caller.
    /// </summary>
    public bool AllowUnaligned { get; private set; }

    protected EmailSendLog()
    {
    }

    // ── Factory ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Cria um registro InFlight (antes de tentar providers).
    /// </summary>
    public static EmailSendLog CreateInFlight(
        string requestId,
        string? idempotencyKey,
        string toHash,
        string subjectHash,
        string? bodyHash,
        string fromDomain)
    {
        return new EmailSendLog
        {
            RequestId = requestId,
            IdempotencyKey = idempotencyKey,
            ToHash = toHash,
            SubjectHash = subjectHash,
            BodyHash = bodyHash,
            FromDomain = fromDomain,
            Status = "InFlight",
            Provider = string.Empty,
            AttemptNo = 0,
            LatencyMs = 0,
            AllowUnaligned = false,
        };
    }

    // ── Domain methods ────────────────────────────────────────────────────────

    /// <summary>
    /// Atualiza o log com o resultado de uma tentativa específica.
    /// </summary>
    public void RecordAttempt(
        string provider,
        int attemptNo,
        bool success,
        string? providerMessageId,
        string? error,
        int latencyMs,
        bool allowUnaligned)
    {
        Provider = provider;
        AttemptNo = attemptNo;
        ProviderMessageId = providerMessageId;
        Error = error;
        LatencyMs = latencyMs;
        AllowUnaligned = allowUnaligned;
        // Status permanece InFlight durante tentativas intermediárias — só as transições
        // terminais (MarkSent/MarkFailed/MarkUncertain) mudam o status. Flipar para
        // "Failed" no meio da cadeia abriria janela para replay concorrente da mesma
        // idempotency key enquanto o dispatch original ainda está em andamento.
        SetUpdated("system");
    }

    /// <summary>
    /// Marca como enviado com sucesso.
    /// </summary>
    public void MarkSent(string provider, string? providerMessageId, bool allowUnaligned)
    {
        Provider = provider;
        ProviderMessageId = providerMessageId;
        AllowUnaligned = allowUnaligned;
        Status = "Sent";
        Error = null;
        SetUpdated("system");
    }

    /// <summary>
    /// Marca como falha final (todos os providers esgotados).
    /// </summary>
    public void MarkFailed()
    {
        Status = "Failed";
        SetUpdated("system");
    }

    /// <summary>
    /// Resultado ambíguo: o provider pode ter aceitado o envio mas a confirmação não
    /// chegou (timeout duro / crash). O caller deve re-tentar com a MESMA idempotency
    /// key ciente do risco de duplicidade.
    /// </summary>
    public void MarkUncertain(string? reason = null)
    {
        Status = "Uncertain";
        if (!string.IsNullOrWhiteSpace(reason))
        {
            Error = reason;
        }
        SetUpdated("system");
    }
}
