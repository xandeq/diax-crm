using Diax.Application.Customers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Diax.Infrastructure.Workers;

/// <summary>
/// Pull agendado de leads do Extrator de Dados — roda 1x/dia na hora configurada
/// (ExtractorPull:DailyHourUtc, default 15 UTC = 12:00 BRT, DEPOIS da janela de envio
/// da manhã). Segue o padrão do LeadScoringWorker: poll periódico + decisão idempotente
/// (o import deduplica por e-mail, então um eventual duplo-run não cria duplicatas).
/// Desligado por default — liga via ExtractorPull:Enabled / DIAX_ExtractorPull__Enabled.
/// </summary>
public class ExtractorPullWorker : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(15);

    /// <summary>1 tentativa + 2 retries por dia; depois desiste até o dia seguinte.</summary>
    private const int MaxAttemptsPerDay = 3;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ExtractorPullOptions _options;
    private readonly ILogger<ExtractorPullWorker> _logger;

    private DateOnly? _lastRunDate;
    private DateOnly? _attemptsDate;
    private int _attemptsToday;

    public ExtractorPullWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<ExtractorPullOptions> options,
        ILogger<ExtractorPullWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation(
                "[ExtractorPull] Worker DESABILITADO (ExtractorPull:Enabled=false). " +
                "Para ligar: DIAX_ExtractorPull__Enabled=true.");
            return;
        }

        _logger.LogInformation(
            "[ExtractorPull] Worker iniciado (1x/dia às {Hour}h UTC, maxPages={MaxPages})",
            _options.DailyHourUtc, _options.MaxPages);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(PollInterval, stoppingToken);

                var now = DateTime.UtcNow;
                var today = DateOnly.FromDateTime(now);

                if (_attemptsDate != today)
                {
                    _attemptsDate = today;
                    _attemptsToday = 0;
                }

                if (now.Hour < _options.DailyHourUtc || _lastRunDate == today)
                    continue;

                _attemptsToday++;
                _logger.LogInformation(
                    "[ExtractorPull] Iniciando pull diário do Extrator (tentativa {Attempt}/{Max})",
                    _attemptsToday, MaxAttemptsPerDay);

                using (var scope = _scopeFactory.CreateScope())
                {
                    var integration = scope.ServiceProvider.GetRequiredService<IExtractorIntegrationService>();
                    var result = await integration.ImportLeadsAsync(
                        maxPages: _options.MaxPages,
                        cancellationToken: stoppingToken);

                    if (result.IsSuccess)
                    {
                        var r = result.Value;
                        _logger.LogInformation(
                            "[ExtractorPull] Pull diário concluído: {Success} sucesso, {Skipped} ignorados, {Failed} falhas (total {Total})",
                            r.SuccessCount, r.SkippedCount, r.FailedCount, r.TotalRecords);
                        _lastRunDate = today;
                    }
                    else if (result.Error.Code == "ExtractorImport.NoLeads")
                    {
                        // Extrator sem lead novo/válido não é falha operacional — considera o dia cumprido.
                        _logger.LogInformation("[ExtractorPull] Nenhum lead válido no Extrator hoje — nada a importar.");
                        _lastRunDate = today;
                    }
                    else
                    {
                        _logger.LogError(
                            "[ExtractorPull] Pull diário falhou ({Code}): {Message}",
                            result.Error.Code, result.Error.Message);
                        GiveUpForTodayIfExhausted(today);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ExtractorPull] Erro inesperado no pull diário.");
                GiveUpForTodayIfExhausted(DateOnly.FromDateTime(DateTime.UtcNow));
            }
        }

        _logger.LogInformation("[ExtractorPull] Worker parado");
    }

    private void GiveUpForTodayIfExhausted(DateOnly today)
    {
        if (_attemptsToday < MaxAttemptsPerDay)
            return;

        _lastRunDate = today; // desiste até amanhã — não martela o Extrator o dia inteiro
        _logger.LogWarning(
            "[ExtractorPull] {Max} tentativas falharam hoje — desistindo até o próximo dia. Investigar causa raiz (token/URL/backend do Extrator).",
            MaxAttemptsPerDay);
    }
}
