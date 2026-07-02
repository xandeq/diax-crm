using Diax.Application.AI.Services;
using Diax.Application.AI.VideoGeneration.Dtos;
using Diax.Application.Common;
using Diax.Domain.AI;
using Diax.Domain.Common;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading;

namespace Diax.Application.AI.VideoGeneration;

public class VideoJobService : IApplicationService, IVideoJobService
{
    private readonly IVideoGenerationJobRepository _jobRepository;
    private readonly IVideoGenerationService _videoGenerationService;
    private readonly IAiModelValidator _aiModelValidator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<VideoJobService> _logger;

    /// <summary>Jobs em Processing há mais tempo que isso são considerados órfãos de um restart.</summary>
    private static readonly TimeSpan StaleProcessingThreshold = TimeSpan.FromMinutes(30);

    public VideoJobService(
        IVideoGenerationJobRepository jobRepository,
        IVideoGenerationService videoGenerationService,
        IAiModelValidator aiModelValidator,
        IUnitOfWork unitOfWork,
        ILogger<VideoJobService> logger)
    {
        _jobRepository = jobRepository;
        _videoGenerationService = videoGenerationService;
        _aiModelValidator = aiModelValidator;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<VideoJobDto> EnqueueAsync(
        VideoGenerationRequestDto request, Guid userId, CancellationToken ct = default)
    {
        // Fail-fast nas validações baratas — o resto (modelo, chave, quota) é validado
        // pelo VideoGenerationService quando o worker processar (com fallback automático).
        if (string.IsNullOrWhiteSpace(request.Prompt) && string.IsNullOrWhiteSpace(request.ReferenceImageBase64))
            throw new ArgumentException("Informe um prompt de texto ou uma imagem de referência para gerar o vídeo.");

        if (request.DurationSeconds is < 1 or > 30)
            throw new ArgumentException("DurationSeconds deve estar entre 1 e 30 segundos.");

        if (request.Width is < 256 or > 4096 || request.Height is < 256 or > 4096)
            throw new ArgumentException("Dimensões devem estar entre 256 e 4096 pixels.");

        var providerKey = request.Provider.ToLower();
        if (!await _aiModelValidator.IsValidProviderAsync(providerKey, ct))
            throw new ArgumentException($"Provedor '{request.Provider}' não está ativo ou não existe.");

        var job = new VideoGenerationJob(
            userId: userId,
            provider: providerKey,
            model: request.Model,
            prompt: request.Prompt,
            negativePrompt: request.NegativePrompt,
            durationSeconds: request.DurationSeconds,
            width: request.Width,
            height: request.Height,
            aspectRatio: request.AspectRatio,
            seed: request.Seed,
            referenceImageBase64: request.ReferenceImageBase64,
            allowFallback: request.AllowFallback);

        await _jobRepository.AddAsync(job, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var position = await _jobRepository.CountQueuedAheadAsync(job.CreatedAt, ct);

        _logger.LogInformation(
            "VideoJob enqueued. JobId: {JobId}. User: {UserId}. Provider: {Provider}. Model: {Model}. QueuePosition: {Position}",
            job.Id, userId, providerKey, request.Model, position);

        return ToDto(job, position);
    }

    public async Task<VideoJobDto?> GetAsync(Guid jobId, Guid userId, CancellationToken ct = default)
    {
        var job = await _jobRepository.GetByIdAsync(jobId, ct);
        if (job == null || job.UserId != userId)
            return null; // não vaza existência de jobs de outros usuários

        int? position = null;
        if (job.Status == VideoGenerationJobStatus.Queued)
            position = await _jobRepository.CountQueuedAheadAsync(job.CreatedAt, ct);

        return ToDto(job, position);
    }

    public async Task<List<VideoJobDto>> ListAsync(Guid userId, int take = 20, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 100);
        var jobs = await _jobRepository.GetByUserIdAsync(userId, take, ct);
        return jobs.Select(j => ToDto(j, null)).ToList();
    }

    public async Task<int> ProcessNextAsync(CancellationToken ct = default)
    {
        var job = await _jobRepository.GetNextQueuedAsync(ct);
        if (job == null)
            return 0;

        // Claim: marca Processing e persiste antes de começar (worker único por instância;
        // o recovery de stale jobs cobre o caso de restart no meio).
        job.MarkProcessing();
        await _jobRepository.UpdateAsync(job, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "VideoJob processing started. JobId: {JobId}. Provider: {Provider}. Model: {Model}",
            job.Id, job.Provider, job.Model);

        try
        {
            var request = new VideoGenerationRequestDto(
                Provider: job.Provider,
                Model: job.Model,
                Prompt: job.Prompt,
                NegativePrompt: job.NegativePrompt,
                DurationSeconds: job.DurationSeconds,
                Width: job.Width,
                Height: job.Height,
                AspectRatio: job.AspectRatio,
                Seed: job.Seed,
                ReferenceImageBase64: job.ReferenceImageBase64,
                AllowFallback: job.AllowFallback);

            // Não repassa o stoppingToken do worker: um shutdown não deve abortar uma
            // geração já em andamento no provider (o recovery marca o job se o app morrer).
            var result = await _videoGenerationService.GenerateAsync(request, job.UserId, CancellationToken.None);

            job.MarkCompleted(
                providerUsed: result.ProviderUsed,
                modelUsed: result.ModelUsed,
                videoUrl: result.VideoUrl,
                thumbnailUrl: result.ThumbnailUrl,
                requestId: result.RequestId,
                durationMs: result.DurationMs,
                fallbackOccurred: result.FallbackOccurred,
                attemptedProvidersJson: SerializeAttempted(result.AttemptedProviders));

            _logger.LogInformation(
                "VideoJob completed. JobId: {JobId}. ProviderUsed: {Provider}. Duration: {Duration}ms. Fallback: {Fallback}",
                job.Id, result.ProviderUsed, result.DurationMs, result.FallbackOccurred);
        }
        catch (Exception ex)
        {
            var category = ex is AiProviderException aiEx ? aiEx.ErrorCode : AiErrorCategory.Unknown;
            job.MarkFailed(ex.Message, category);

            _logger.LogError(ex,
                "VideoJob failed. JobId: {JobId}. Provider: {Provider}. Category: {Category}",
                job.Id, job.Provider, category);
        }

        await _jobRepository.UpdateAsync(job, CancellationToken.None);
        await _unitOfWork.SaveChangesAsync(CancellationToken.None);
        return 1;
    }

    public async Task RecoverStaleJobsAsync(CancellationToken ct = default)
    {
        var stale = await _jobRepository.GetStaleProcessingAsync(StaleProcessingThreshold, ct);
        if (stale.Count == 0)
            return;

        foreach (var job in stale)
        {
            job.MarkFailed(
                "Geração interrompida por reinício do servidor. Tente novamente.",
                AiErrorCategory.Timeout);
            await _jobRepository.UpdateAsync(job, ct);
            _logger.LogWarning("VideoJob stale recovered as Failed. JobId: {JobId}", job.Id);
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }

    private static string? SerializeAttempted(List<string>? attempted) =>
        attempted is { Count: > 0 } ? JsonSerializer.Serialize(attempted) : null;

    private static List<string>? DeserializeAttempted(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static VideoJobDto ToDto(VideoGenerationJob job, int? queuePosition) => new(
        Id: job.Id,
        Status: job.Status,
        Provider: job.Provider,
        Model: job.Model,
        ProviderUsed: job.ProviderUsed,
        ModelUsed: job.ModelUsed,
        VideoUrl: job.VideoUrl,
        ThumbnailUrl: job.ThumbnailUrl,
        ErrorMessage: job.ErrorMessage,
        ErrorCategory: job.ErrorCategory,
        FallbackOccurred: job.FallbackOccurred,
        AttemptedProviders: DeserializeAttempted(job.AttemptedProvidersJson),
        QueuePosition: queuePosition,
        DurationMs: job.DurationMs,
        CreatedAt: job.CreatedAt,
        StartedAt: job.StartedAt,
        CompletedAt: job.CompletedAt,
        EstimatedCostUsd: job.Status == VideoGenerationJobStatus.Completed
            ? AiGenerationCostEstimator.EstimateVideoCostUsd(
                job.ProviderUsed ?? job.Provider, job.ModelUsed ?? job.Model, job.DurationSeconds)
            : null);
}
