namespace Diax.Application.Notifications;

/// <summary>Envio de mensagens ao Telegram do dono do CRM (bot configurado em Telegram:BotToken/ChatId).</summary>
public interface ITelegramSender
{
    bool IsConfigured { get; }

    /// <summary>Envia texto (parse_mode HTML). Retorna false se não configurado ou se a API falhar (logado).</summary>
    Task<bool> SendAsync(string htmlText, CancellationToken cancellationToken = default);
}
