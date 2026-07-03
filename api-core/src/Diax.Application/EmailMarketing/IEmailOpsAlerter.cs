namespace Diax.Application.EmailMarketing;

/// <summary>
/// Alertas operacionais do módulo de email (dead-letter, breaker aberto).
/// Implementação com cooldown por chave para não inundar o canal — perder um alerta
/// repetido é aceitável; spam torna o canal inútil.
/// </summary>
public interface IEmailOpsAlerter
{
    /// <summary>
    /// Envia um alerta HTML. <paramref name="throttleKey"/> agrupa alertas do mesmo tipo:
    /// dentro da janela de cooldown, chamadas com a mesma chave viram no-op.
    /// Nunca lança — falha de alerta não pode derrubar o ciclo de envio.
    /// </summary>
    Task NotifyAsync(string throttleKey, string htmlMessage, CancellationToken cancellationToken = default);
}
