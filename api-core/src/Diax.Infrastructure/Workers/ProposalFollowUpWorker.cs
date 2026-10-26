using Diax.Infrastructure.Data;
using Diax.Application.Customers;
using Diax.Domain.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Workers;

/// <summary>
/// Gera as tarefas de follow-up de proposta todo dia útil ~09h BRT (12h UTC).
/// Mesmo padrão do CommercialBriefingWorker: poll periódico + job_runs (1x/dia, sobrevive a deploy).
/// Dono resolvido por Integrations:DefaultUserId (fallback: Auth:AdminEmail).
/// </summary>
public class ProposalFollowUpWorker : BackgroundService
{
    private const int RunAtUtcHour = 12;
    private const int RunAtUtcMinute = 0;
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(10);

    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProposalFollowUpWorker> _logger;
    private const string JobName = "proposal-followup";
    private DateOnly? _lastRunDate;
    private DateOnly? _lastFailureAlertDate;

    public ProposalFollowUpWorker(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<ProposalFollowUpWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[ProposalFollowUp] Worker iniciado (dias úteis ~{Hour:D2}:{Minute:D2} UTC)", RunAtUtcHour, RunAtUtcMinute);

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
                    _lastRunDate = today; // fim de semana: marca como feito sem rodar
                    continue;
                }

                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DiaxDbContext>();
                if (await DailyJobLedger.HasRunAsync(db, JobName, today, stoppingToken))
                {
                    _lastRunDate = today; // já rodou hoje (antes de um recycle/deploy)
                    continue;
                }

                var ownerId = await ResolveOwnerAsync(scope.ServiceProvider, stoppingToken);
                if (ownerId == null)
                {
                    _logger.LogWarning("[ProposalFollowUp] Dono não resolvido (Integrations:DefaultUserId/Auth:AdminEmail) — pulando hoje");
                    _lastRunDate = today;
                    continue;
                }

                var service = scope.ServiceProvider.GetRequiredService<ProposalFollowUpService>();
                var created = await service.RunAsync(ownerId.Value, now, stoppingToken);
                _logger.LogInformation("[ProposalFollowUp] Rodada diária: {Created} tarefa(s)", created);

                await DailyJobLedger.MarkRunAsync(db, JobName, today, stoppingToken);
                _lastRunDate = today;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ProposalFollowUp] Erro na rodada diária — tentará no próximo ciclo");
                await TryAlertFailureAsync(ex, stoppingToken);
            }
        }

        _logger.LogInformation("[ProposalFollowUp] Worker parado");
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
            msg = msg.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
            await telegram.SendAsync(
                $"⚠️ <b>DIAX CRM</b>: worker do follow-up de propostas falhou.\n<code>{msg}</code>\nVai tentar de novo no próximo ciclo.", ct);
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
