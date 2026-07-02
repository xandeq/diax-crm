using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Diax.Infrastructure.Email;

/// <summary>
/// Loop de agendamento da fila de emails. Toda a lógica de despacho vive em
/// <see cref="EmailQueueCycleProcessor"/> (scoped, testável) — aqui só o timer.
/// </summary>
public class EmailQueueProcessorWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailQueueProcessorWorker> _logger;

    public EmailQueueProcessorWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<EmailSettings> settings,
        ILogger<EmailQueueProcessorWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EmailQueueProcessorWorker iniciado. Intervalo: {Interval} minuto(s)", _settings.DispatchIntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<EmailQueueCycleProcessor>();
                await processor.ProcessOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao processar fila de e-mails.");
            }

            var delayMinutes = _settings.DispatchIntervalMinutes <= 0 ? 5 : _settings.DispatchIntervalMinutes;
            await Task.Delay(TimeSpan.FromMinutes(delayMinutes), stoppingToken);
        }
    }
}
