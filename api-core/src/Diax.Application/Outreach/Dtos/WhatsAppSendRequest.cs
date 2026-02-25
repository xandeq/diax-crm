namespace Diax.Application.Outreach.Dtos;

/// <summary>
/// Request para envio individual de WhatsApp.
/// </summary>
public record WhatsAppSendRequest
{
    /// <summary>
    /// ID do cliente/lead para envio (usa o WhatsApp cadastrado).
    /// </summary>
    public Guid? CustomerId { get; init; }

    /// <summary>
    /// Número de telefone alternativo (usado se CustomerId não informado).
    /// </summary>
    public string? PhoneNumber { get; init; }

    /// <summary>
    /// Texto da mensagem a enviar.
    /// </summary>
    public string Message { get; init; } = "";
}
