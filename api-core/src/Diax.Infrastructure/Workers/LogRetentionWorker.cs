using Diax.Application.Audit;
using Diax.Application.ErrorLogs;
using Diax.Application.Logs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Workers;

/// <summary>
/// Apaga audit/app/error logs mais velhos que <c>LogRetention:Days</c>, 1x/dia.
/// DESLIGADO por padrão: sem a config (ou com valor &lt; 30) não apaga nada.
/// </summary>
public class LogRetentionWorker : BackgroundService
{
    public const string ConfigKey = "LogRetention:Days";
    private const int MinRetentionDays = 30;
    private const int RunAtUtcHour = 7; // 04h America/Sao_Paulo, fora do horário de envio
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(15);

    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LogRetentionWorker> _logger;
    private DateOnly? _lastRunDate;

    public LogRetentionWorker(IServiceProvider serviceProvider, IConfiguration configuration, ILogger<LogRetentionWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    public static int? ResolveRetentionDays(IConfiguration configuration)
        => int.TryParse(configuration[ConfigKey], out var days) && days >= MinRetentionDays ? days : null;

    public static async Task<(int Audit, int App, int Error)> RunCleanupAsync(
        IServiceProvider scopedProvider, int retentionDays, CancellationToken ct)
    {
        var audit = await scopedProvider.GetRequiredService<IAuditLogService>().CleanupOldLogsAsync(retentionDays, ct);
        var app = await scopedProvider.GetRequiredService<IAppLogService>().CleanupAsync(retentionDays, ct);
        var error = await scopedProvider.GetRequiredService<IErrorLogService>().CleanupAsync(retentionDays, ct);

        return (audit.IsSuccess ? audit.Value : 0, app.IsSuccess ? app.Value : 0, error.IsSuccess ? error.Value : 0);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (ResolveRetentionDays(_configuration) is null)
            _logger.LogInformation("[LogRetention] Desligado ({Key} ausente ou < {Min}) — nenhum log será apagado", ConfigKey, MinRetentionDays);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(PollInterval, stoppingToken);

                var now = DateTime.UtcNow;
                var today = DateOnly.FromDateTime(now);
                if (now.Hour < RunAtUtcHour || _lastRunDate == today)
                    continue;

                // Relido a cada ciclo: dá pra mudar/desligar a janela pela config sem redeploy.
                var days = ResolveRetentionDays(_configuration);
                if (days is null)
                {
                    _lastRunDate = today;
                    continue;
                }

                using var scope = _serviceProvider.CreateScope();
                var (audit, app, error) = await RunCleanupAsync(scope.ServiceProvider, days.Value, stoppingToken);
                _logger.LogInformation(
                    "[LogRetention] Retenção {Days}d: removidos {Audit} audit, {App} app, {Error} error logs",
                    days.Value, audit, app, error);

                _lastRunDate = today;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[LogRetention] Erro no cleanup — tentará no próximo ciclo");
            }
        }
    }
}
