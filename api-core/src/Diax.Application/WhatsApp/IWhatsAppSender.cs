namespace Diax.Application.WhatsApp;

/// <summary>
/// Interface para envio de mensagens WhatsApp.
/// </summary>
public interface IWhatsAppSender
{
    /// <summary>
    /// Envia uma mensagem de texto via WhatsApp.
    /// </summary>
    /// <param name="whatsappNumber">Número do WhatsApp (ex: 5527920010738)</param>
    /// <param name="message">Texto da mensagem</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    Task<WhatsAppSendResult> SendTextAsync(string whatsappNumber, string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se a instância do WhatsApp está conectada.
    /// </summary>
    Task<WhatsAppConnectionStatus> GetConnectionStatusAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Resultado do envio de uma mensagem WhatsApp.
/// </summary>
public record WhatsAppSendResult(bool Success, string? MessageId, string? Error);

/// <summary>
/// Status da conexão da instância WhatsApp.
/// </summary>
public record WhatsAppConnectionStatus(bool IsConnected, string State, string? InstanceName);
