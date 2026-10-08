using Diax.Application.Customers;
using Diax.Application.Customers.Dtos;
using Diax.Application.Notifications;
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
    private string? _lastError;

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

                        var alert = BuildZeroImportAlert(r);
                        if (alert != null)
                        {
                            _logger.LogWarning(
                                "[ExtractorPull] Pull terminou com 0 importados e {Failed} falhas — alertando no Telegram",
                                r.FailedCount);
                            await SendAlertAsync(scope.ServiceProvider, alert, stoppingToken);
                        }
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
                        _lastError = $"{result.Error.Code}: {result.Error.Message}";
                        await GiveUpForTodayIfExhaustedAsync(today, stoppingToken);
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
                _lastError = $"{ex.GetType().Name}: {ex.Message}";
                await GiveUpForTodayIfExhaustedAsync(DateOnly.FromDateTime(DateTime.UtcNow), stoppingToken);
            }
        }

        _logger.LogInformation("[ExtractorPull] Worker parado");
    }

    private async Task GiveUpForTodayIfExhaustedAsync(DateOnly today, CancellationToken cancellationToken)
    {
        if (_attemptsToday < MaxAttemptsPerDay)
            return;

        _lastRunDate = today; // desiste até amanhã — não martela o Extrator o dia inteiro
        _logger.LogWarning(
            "[ExtractorPull] {Max} tentativas falharam hoje — desistindo até o próximo dia. Investigar causa raiz (token/URL/backend do Extrator).",
            MaxAttemptsPerDay);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            await SendAlertAsync(scope.ServiceProvider, BuildGiveUpAlert(MaxAttemptsPerDay, _lastError), cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "[ExtractorPull] Falha ao enviar alerta de desistência (non-fatal)");
        }
    }

    /// <summary>
    /// Alerta só para o padrão "pull rodou mas não entrou NADA e houve falhas" (05-07/10/2026:
    /// 1000 falhas por lead sem e-mail / crash de notes). Dia com ao menos 1 importado, ou com
    /// tudo ignorado (duplicado) sem falha, NÃO alerta. Retorna null quando não há alerta.
    /// </summary>
    public static string? BuildZeroImportAlert(BulkImportResponse r)
    {
        if (r.SuccessCount > 0 || r.FailedCount == 0)
            return null;

        var reasons = (r.Errors ?? new List<ImportError>())
            .GroupBy(e => e.ErrorMessage)
            .OrderByDescending(g => g.Count())
            .Take(3)
            .Select(g => $"• {EscapeHtml(g.Key)} ({g.Count()}x)");

        return "⚠️ <b>DIAX CRM — PULL do Extrator importou 0 leads</b>\n" +
               $"0 importados, {r.SkippedCount} ignorados, {r.FailedCount} falhas (de {r.TotalRecords}).\n" +
               "Principais motivos:\n" + string.Join("\n", reasons) + "\n" +
               "Logs: diax-api-AAAAMMDD.log, filtro [ExtractorPull]|Importação.";
    }

    /// <summary>Alerta quando as 3 tentativas do dia falharam (token, URL, Extrator fora do ar, exceção).</summary>
    public static string BuildGiveUpAlert(int attempts, string? lastError) =>
        "🔴 <b>DIAX CRM — PULL do Extrator falhou</b>\n" +
        $"{attempts} tentativas falharam hoje; nenhum lead importado. Próxima tentativa amanhã 12h BRT.\n" +
        $"Último erro: {EscapeHtml(lastError ?? "desconhecido")}";

    /// <summary>Escape mínimo p/ parse_mode HTML do Telegram (mantém acentos legíveis).</summary>
    private static string EscapeHtml(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    private async Task SendAlertAsync(IServiceProvider services, string message, CancellationToken cancellationToken)
    {
        try
        {
            var telegram = services.GetService<ITelegramSender>();
            if (telegram == null || !telegram.IsConfigured)
            {
                _logger.LogWarning("[ExtractorPull] Telegram não configurado — alerta não enviado: {Message}", message);
                return;
            }
            await telegram.SendAsync(message, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "[ExtractorPull] Falha ao enviar alerta no Telegram (non-fatal)");
        }
    }
}
