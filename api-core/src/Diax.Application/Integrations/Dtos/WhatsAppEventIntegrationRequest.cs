namespace Diax.Application.Integrations.Dtos;

/// <summary>
/// Callback do sender externo (n8n + WAHA) reportando de volta ao CRM o resultado
/// de uma mensagem de WhatsApp: envio confirmado, resposta do lead, opt-out ou falha.
/// O CRM é a fonte de verdade do outreach — este endpoint fecha o loop.
/// </summary>
public sealed class WhatsAppEventIntegrationRequest
{
    /// <summary>Id do customer no CRM (preferencial). Se ausente, resolve por <see cref="Phone"/>.</summary>
    public Guid? CustomerId { get; init; }

    /// <summary>Telefone em qualquer formato (E.164, JID do WAHA "5511999998888@c.us", com máscara, etc.).</summary>
    public string? Phone { get; init; }

    /// <summary>"sent" | "reply" | "optout" | "failed".</summary>
    public string? Event { get; init; }

    /// <summary>Texto da mensagem enviada ou da resposta recebida (opcional).</summary>
    public string? Text { get; init; }

    /// <summary>Provider do sender (ex.: "waha").</summary>
    public string? Provider { get; init; }

    /// <summary>Id da mensagem no provider (opcional, para correlação/log).</summary>
    public string? MessageId { get; init; }
}
