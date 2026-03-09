namespace Diax.Shared.Ai;

public record VideoGenerationOptions(
    string ApiKey,
    string BaseUrl,
    string Model,
    int? DurationSeconds = 5,
    int Width = 1280,
    int Height = 720,
    string? AspectRatio = null,
    string? NegativePrompt = null,
    string? Seed = null
);

public record VideoGenerationResult(
    string VideoUrl,
    string? ThumbnailUrl,
    int? DurationMs
);

public interface IAiVideoGenerationClient
{
    string ProviderName { get; }
    bool SupportsImageToVideo { get; }

    Task<VideoGenerationResult> GenerateAsync(
        string? prompt,
        VideoGenerationOptions options,
        string? referenceImageBase64 = null,
        CancellationToken ct = default);
}
