namespace Diax.Domain.EmailMarketing.Enums;

public enum EmailQueueStatus
{
    Queued = 0,
    Processing = 1,
    Sent = 2,
    Failed = 3,

    /// <summary>
    /// Esgotou as tentativas em todos os retries — fora da rotação de retry.
    /// Diferente de Failed (que ainda volta via retry), DeadLettered é terminal
    /// até um requeue manual. Sem este estado, itens exauridos ficavam como
    /// Failed invisível = email perdido em silêncio (auditoria §7/§12).
    /// </summary>
    DeadLettered = 4
}
