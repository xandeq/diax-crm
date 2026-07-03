using System.Collections.Concurrent;
using Diax.Application.EmailMarketing;
using Diax.Application.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Diax.Infrastructure.Email;

/// <summary>
/// Alertas operacionais de email via Telegram (bot já usado pelo briefing comercial).
/// Cooldown in-memory por chave: o pior cenário (recycle zera o cooldown) é um alerta
/// duplicado — aceitável. Nunca lança: envio de alerta não pode derrubar o worker.
/// </summary>
public class TelegramEmailOpsAlerter : IEmailOpsAlerter
{
    private static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(30);
    private static readonly ConcurrentDictionary<string, DateTime> LastSentByKey = new();

    private readonly ITelegramSender _telegram;
    private readonly EmailSettings _settings;
    private readonly ILogger<TelegramEmailOpsAlerter> _logger;

    public TelegramEmailOpsAlerter(
        ITelegramSender telegram,
        IOptions<EmailSettings> settings,
        ILogger<TelegramEmailOpsAlerter> logger)
    {
        _telegram = telegram;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task NotifyAsync(string throttleKey, string htmlMessage, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_settings.OpsAlertsEnabled || !_telegram.IsConfigured)
            {
                return;
            }

            var now = DateTime.UtcNow;
            var last = LastSentByKey.GetOrAdd(throttleKey, DateTime.MinValue);
            if (now - last < Cooldown)
            {
                return;
            }

            // Corrida entre dois ciclos é inofensiva (no máximo 1 alerta extra).
            LastSentByKey[throttleKey] = now;

            var sent = await _telegram.SendAsync($"📧 <b>Email Ops</b>\n{htmlMessage}", cancellationToken);
            if (!sent)
            {
                _logger.LogWarning("Alerta de email ops não enviado (Telegram indisponível). Key={Key}", throttleKey);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao enviar alerta de email ops. Key={Key}", throttleKey);
        }
    }
}
