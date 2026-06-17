namespace Diax.Application.EmailMarketing;

/// <summary>
/// Define os comportamentos do Circuit Breaker para a campanha piloto de e-mails.
/// </summary>
public interface IPilotCircuitBreaker
{
    /// <summary>Indica se o circuito está aberto (bloqueando disparos).</summary>
    bool IsOpen { get; }

    /// <summary>Motivo da abertura do circuito.</summary>
    string? Reason { get; }

    /// <summary>Abre o circuito sob um determinado motivo.</summary>
    void Open(string reason);

    /// <summary>Fecha/restaura o circuito limpando falhas.</summary>
    void Reset();

    /// <summary>Registra um envio de e-mail bem-sucedido.</summary>
    void RecordSuccess();

    /// <summary>Registra uma falha de envio de e-mail.</summary>
    void RecordFailure(string errorMsg);

    /// <summary>Registra uma falha no recebimento/processamento de webhook.</summary>
    void RecordWebhookFailure();

    /// <summary>Reseta contagem de falhas de webhook.</summary>
    void ResetWebhookFailure();

    /// <summary>Quantidade acumulada de falhas de webhook consecutivas.</summary>
    int WebhookFailureCount { get; }

    /// <summary>Taxa atual de erro de disparos (em percentual de 0 a 100).</summary>
    double CurrentErrorRate { get; }
}
