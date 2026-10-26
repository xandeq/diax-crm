using Diax.Infrastructure.Data;
using Diax.Application.Customers;
using Diax.Domain.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Workers;

/// <summary>
/// Envia o briefing comercial no Telegram todo dia útil ~07h30 BRT (10h30 UTC).
/// Padrão QuotaResetWorker: poll periódico + decisão idempotente por data.
/// Dono resolvido por Integrations:DefaultUserId (fallback: Auth:AdminEmail).
/// </summary>
public class CommercialBriefingWorker : BackgroundService
{
    private const int RunAtUtcHour = 10;
    private const int RunAtUtcMinute = 30;
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(10);

    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CommercialBriefingWorker> _logger;
    private const string JobName = "commercial-briefing";
    private DateOnly? _lastRunDate;
    private DateOnly? _lastFailureAlertDate;
    private DateOnly? _attemptsDate;
    private int _attemptsToday;

    /// <summary>Tentativas de envio por dia (1 por ciclo de 10 min) antes de desistir até amanhã.</summary>
    public const int MaxAttemptsPerDay = 3;

    /// <summary>
    /// Só marca o dia como feito quando o briefing saiu, ou quando as tentativas do dia acabaram.
    /// Falha de envio (ex.: Telegram fora) tenta de novo no próximo ciclo.
    /// </summary>
    public static bool ShouldMarkRun(bool sent, int attemptsToday) =>
        sent || attemptsToday >= MaxAttemptsPerDay;

    public CommercialBriefingWorker(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<CommercialBriefingWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[Briefing] Worker iniciado (dias úteis ~{Hour:D2}:{Minute:D2} UTC)", RunAtUtcHour, RunAtUtcMinute);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(PollInterval, stoppingToken);

                var now = DateTime.UtcNow;
                var today = DateOnly.FromDateTime(now);
                var brtDay = (now.AddHours(-3)).DayOfWeek;

                if (_lastRunDate == today) continue;
                if (now.Hour < RunAtUtcHour || (now.Hour == RunAtUtcHour && now.Minute < RunAtUtcMinute)) continue;
                if (brtDay is DayOfWeek.Saturday or DayOfWeek.Sunday)
                {
                    _lastRunDate = today; // fim de semana: marca como feito sem enviar
                    continue;
                }

                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DiaxDbContext>();
                if (await DailyJobLedger.HasRunAsync(db, JobName, today, stoppingToken))
                {
                    _lastRunDate = today; // já enviado hoje (antes de um recycle/deploy)
                    continue;
                }

                var ownerId = await ResolveOwnerAsync(scope.ServiceProvider, stoppingToken);
                if (ownerId == null)
                {
                    _logger.LogWarning("[Briefing] Dono não resolvido (Integrations:DefaultUserId/Auth:AdminEmail) — pulando hoje");
                    _lastRunDate = today;
                    continue;
                }

                if (_attemptsDate != today)
                {
                    _attemptsDate = today;
                    _attemptsToday = 0;
                }
                _attemptsToday++;

                var service = scope.ServiceProvider.GetRequiredService<CommercialBriefingService>();
                var result = await service.SendAsync(ownerId.Value, stoppingToken);
                if (!ShouldMarkRun(result.IsSuccess, _attemptsToday))
                {
                    _logger.LogWarning("[Briefing] Envio falhou (tentativa {Attempt}/{Max}): {Error} — tenta de novo no próximo ciclo",
                        _attemptsToday, MaxAttemptsPerDay, result.Error.Message);
                    continue;
                }

                if (result.IsSuccess)
                    _logger.LogInformation("[Briefing] Envio diário: OK (tentativa {Attempt})", _attemptsToday);
                else
                    _logger.LogError("[Briefing] Envio falhou {Max}x hoje: {Error} — desistindo até amanhã",
                        MaxAttemptsPerDay, result.Error.Message);

                await DailyJobLedger.MarkRunAsync(db, JobName, today, stoppingToken);
                _lastRunDate = today;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Briefing] Erro no envio diário — tentará no próximo ciclo");
                await TryAlertFailureAsync(ex, stoppingToken);
            }
        }

        _logger.LogInformation("[Briefing] Worker parado");
    }

    /// <summary>Alerta o dono no Telegram sobre falha do worker (máx 1 aviso/dia, fire-safe).</summary>
    private async Task TryAlertFailureAsync(Exception ex, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (_lastFailureAlertDate == today) return;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var telegram = scope.ServiceProvider.GetRequiredService<Diax.Application.Notifications.ITelegramSender>();
            var msg = ex.Message.Length > 200 ? ex.Message[..200] : ex.Message;
            await telegram.SendAsync(
                $"⚠️ <b>DIAX CRM</b>: worker do briefing comercial falhou.\n<code>{msg}</code>\nVai tentar de novo no próximo ciclo.", ct);
            _lastFailureAlertDate = today;
        }
        catch { /* alerta é best-effort */ }
    }

    private async Task<Guid?> ResolveOwnerAsync(IServiceProvider sp, CancellationToken ct)
    {
        var configured = _configuration["Integrations:DefaultUserId"];
        if (Guid.TryParse(configured, out var id))
            return id;

        var adminEmail = _configuration["Auth:AdminEmail"];
        if (!string.IsNullOrWhiteSpace(adminEmail))
        {
            var users = sp.GetRequiredService<IUserRepository>();
            var admin = await users.GetByEmailAsync(adminEmail, ct);
            return admin?.Id;
        }

        return null;
    }
}
