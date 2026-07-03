using Diax.Application.Customers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Workers;

/// <summary>
/// Recalcula o lead scoring diariamente (~06h BRT = 09h UTC) — segue o padrão do
/// QuotaResetWorker: poll periódico e decisão idempotente. Não cria follow-ups
/// (tasks são do usuário — criadas pelo endpoint autenticado do pipeline).
/// </summary>
public class LeadScoringWorker : BackgroundService
{
    private const int RunAtUtcHour = 9; // 06h America/Sao_Paulo
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(15);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LeadScoringWorker> _logger;
    private DateOnly? _lastRunDate;

    public LeadScoringWorker(IServiceProvider serviceProvider, ILogger<LeadScoringWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[LeadScoring] Background worker started (roda 1x/dia às {Hour}h UTC)", RunAtUtcHour);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(PollInterval, stoppingToken);

                var now = DateTime.UtcNow;
                var today = DateOnly.FromDateTime(now);
                if (now.Hour < RunAtUtcHour || _lastRunDate == today)
                    continue;

                _logger.LogInformation("[LeadScoring] Iniciando recálculo diário de scores");
                using (var scope = _serviceProvider.CreateScope())
                {
                    var scoring = scope.ServiceProvider.GetRequiredService<LeadScoringService>();
                    var summary = await scoring.RecomputeAllAsync(followUpOwnerUserId: null, stoppingToken);
                    _logger.LogInformation(
                        "[LeadScoring] Diário concluído: {Total} leads (Hot {Hot} / Warm {Warm} / Cold {Cold})",
                        summary.LeadsScored, summary.Hot, summary.Warm, summary.Cold);
                }

                _lastRunDate = today;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[LeadScoring] Erro no recálculo diário — tentará no próximo ciclo");
            }
        }

        _logger.LogInformation("[LeadScoring] Background worker stopped");
    }
}
