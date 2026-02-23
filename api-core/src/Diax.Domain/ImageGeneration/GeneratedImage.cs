using Diax.Domain.AI;
using Diax.Domain.Common;

namespace Diax.Domain.ImageGeneration;

public class GeneratedImage : Entity
{
    public Guid ProjectId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid ProviderId { get; private set; }
    public Guid ModelId { get; private set; }
    public string Prompt { get; private set; }
    public string? RevisedPrompt { get; private set; }
    public string? StorageUrl { get; private set; }
    public string? ProviderUrl { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    public string? Seed { get; private set; }
    public string? MetadataJson { get; private set; }
    public decimal? EstimatedCost { get; private set; }
    public int DurationMs { get; private set; }
    public bool Success { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation properties
    public ImageGenerationProject Project { get; private set; } = null!;
    public AiProvider Provider { get; private set; } = null!;
    public AiModel Model { get; private set; } = null!;

    private GeneratedImage() { } // EF Core

    public GeneratedImage(
        Guid projectId,
        Guid userId,
        Guid providerId,
        Guid modelId,
        string prompt,
        int width,
        int height,
        int durationMs,
        bool success,
        string? revisedPrompt = null,
        string? storageUrl = null,
        string? providerUrl = null,
        string? seed = null,
        string? metadataJson = null,
        decimal? estimatedCost = null,
        string? errorMessage = null)
    {
        ProjectId = projectId;
        UserId = userId;
        ProviderId = providerId;
        ModelId = modelId;
        Prompt = prompt;
        Width = width;
        Height = height;
        DurationMs = durationMs;
        Success = success;
        RevisedPrompt = revisedPrompt;
        StorageUrl = storageUrl;
        ProviderUrl = providerUrl;
        Seed = seed;
        MetadataJson = metadataJson;
        EstimatedCost = estimatedCost;
        ErrorMessage = errorMessage;
        CreatedAt = DateTime.UtcNow;
    }

    public void SetStorageUrl(string storageUrl)
    {
        StorageUrl = storageUrl;
    }
}
