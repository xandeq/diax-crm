using Diax.Domain.Common;

namespace Diax.Domain.AI;

/// <summary>
/// Job assíncrono de geração de vídeo. Providers de vídeo podem levar vários minutos
/// (cold start do HuggingFace passa de 6 min) — segurar o request HTTP aberto estoura
/// o timeout do IIS. O fluxo é: enfileira → worker processa → frontend consulta status.
/// </summary>
public class VideoGenerationJob : Entity
{
    public Guid UserId { get; private set; }

    // Parâmetros do pedido
    public string Provider { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public string? Prompt { get; private set; }
    public string? NegativePrompt { get; private set; }
    public int? DurationSeconds { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    public string? AspectRatio { get; private set; }
    public string? Seed { get; private set; }
    public string? ReferenceImageBase64 { get; private set; }
    public bool AllowFallback { get; private set; }

    // Estado do job
    public string Status { get; private set; } = VideoGenerationJobStatus.Queued;
    public string? ProviderUsed { get; private set; }
    public string? ModelUsed { get; private set; }
    public string? VideoUrl { get; private set; }
    public string? ThumbnailUrl { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? ErrorCategory { get; private set; }
    public bool FallbackOccurred { get; private set; }
    public string? AttemptedProvidersJson { get; private set; }
    public string? RequestId { get; private set; }
    public int? DurationMs { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private VideoGenerationJob() { } // EF Core

    public VideoGenerationJob(
        Guid userId,
        string provider,
        string model,
        string? prompt,
        string? negativePrompt,
        int? durationSeconds,
        int width,
        int height,
        string? aspectRatio,
        string? seed,
        string? referenceImageBase64,
        bool allowFallback)
    {
        UserId = userId;
        Provider = provider;
        Model = model;
        Prompt = prompt;
        NegativePrompt = negativePrompt;
        DurationSeconds = durationSeconds;
        Width = width;
        Height = height;
        AspectRatio = aspectRatio;
        Seed = seed;
        ReferenceImageBase64 = referenceImageBase64;
        AllowFallback = allowFallback;
        Status = VideoGenerationJobStatus.Queued;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkProcessing()
    {
        Status = VideoGenerationJobStatus.Processing;
        StartedAt = DateTime.UtcNow;
    }

    public void MarkCompleted(
        string providerUsed,
        string modelUsed,
        string videoUrl,
        string? thumbnailUrl,
        string? requestId,
        int durationMs,
        bool fallbackOccurred,
        string? attemptedProvidersJson)
    {
        Status = VideoGenerationJobStatus.Completed;
        ProviderUsed = providerUsed;
        ModelUsed = modelUsed;
        VideoUrl = videoUrl;
        ThumbnailUrl = thumbnailUrl;
        RequestId = requestId;
        DurationMs = durationMs;
        FallbackOccurred = fallbackOccurred;
        AttemptedProvidersJson = attemptedProvidersJson;
        CompletedAt = DateTime.UtcNow;
        // Base64 de referência pode ter MBs — não precisamos mais dele após concluir.
        ReferenceImageBase64 = null;
    }

    public void MarkFailed(string errorMessage, string? errorCategory, string? attemptedProvidersJson = null)
    {
        Status = VideoGenerationJobStatus.Failed;
        ErrorMessage = errorMessage.Length > 1000 ? errorMessage[..1000] : errorMessage;
        ErrorCategory = errorCategory;
        AttemptedProvidersJson = attemptedProvidersJson;
        CompletedAt = DateTime.UtcNow;
        ReferenceImageBase64 = null;
    }
}

public static class VideoGenerationJobStatus
{
    public const string Queued = "Queued";
    public const string Processing = "Processing";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
}
