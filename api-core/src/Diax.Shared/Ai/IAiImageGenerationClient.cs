namespace Diax.Shared.Ai;

public record ImageGenerationOptions(
    string ApiKey,
    string BaseUrl,
    string Model,
    int Width = 1024,
    int Height = 1024,
    int NumberOfImages = 1,
    string? NegativePrompt = null,
    string? Seed = null,
    string? Style = null,
    string? Quality = null
);

public record ImageGenerationResult(
    string ImageUrl,
    bool IsBase64,
    string? RevisedPrompt,
    string? Seed
);

public interface IAiImageGenerationClient
{
    string ProviderName { get; }
    bool SupportsImageToImage { get; }

    Task<List<ImageGenerationResult>> GenerateAsync(
        string? prompt,
        ImageGenerationOptions options,
        string? referenceImageBase64 = null,
        CancellationToken ct = default);
}
