using Diax.Application.AI.QuotaManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.AI.QuotaManagement;

/// <summary>
/// Background worker that resets daily AI provider quotas at midnight UTC.
/// Runs every 5 minutes to check if daily quotas need resetting.
/// </summary>
public class QuotaResetWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<QuotaResetWorker> _logger;

    public QuotaResetWorker(IServiceProvider serviceProvider, ILogger<QuotaResetWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[QuotaReset] Background worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Check every 5 minutes if we need to reset quotas
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

                _logger.LogDebug("[QuotaReset] Running daily quota reset check");

                using (var scope = _serviceProvider.CreateScope())
                {
                    var quotaService = scope.ServiceProvider.GetRequiredService<IAiQuotaService>();
                    await quotaService.ResetDailyQuotasAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("[QuotaReset] Worker shutdown requested");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[QuotaReset] Error during quota reset check");
                // Don't throw, continue the loop to retry next interval
            }
        }

        _logger.LogInformation("[QuotaReset] Background worker stopped");
    }
}
