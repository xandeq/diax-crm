using Diax.Application.AI.VideoGeneration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Ai;

/// <summary>
/// Worker que processa a fila de geração de vídeo (video_generation_jobs).
/// Um job por vez (providers de vídeo são pesados); quando a fila esvazia, poll a cada 5s.
/// No startup, marca como Failed jobs presos em Processing por um restart anterior.
/// </summary>
public class VideoJobWorker : BackgroundService
{
    private static readonly TimeSpan IdlePollInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ErrorBackoff = TimeSpan.FromSeconds(30);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<VideoJobWorker> _logger;

    public VideoJobWorker(IServiceProvider serviceProvider, ILogger<VideoJobWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[VideoJobWorker] Background worker started");

        // Recovery de jobs órfãos de um restart (best-effort; não impede o worker de subir)
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var jobService = scope.ServiceProvider.GetRequiredService<IVideoJobService>();
            await jobService.RecoverStaleJobsAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[VideoJobWorker] Stale job recovery failed on startup");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                int processed;
                using (var scope = _serviceProvider.CreateScope())
                {
                    var jobService = scope.ServiceProvider.GetRequiredService<IVideoJobService>();
                    processed = await jobService.ProcessNextAsync(stoppingToken);
                }

                // Fila vazia → espera; tinha job → drena imediatamente o próximo
                if (processed == 0)
                    await Task.Delay(IdlePollInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[VideoJobWorker] Error processing video job queue");
                try
                {
                    await Task.Delay(ErrorBackoff, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        _logger.LogInformation("[VideoJobWorker] Background worker stopped");
    }
}
