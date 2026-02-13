using System.Text.Json.Serialization;

namespace Diax.Application.AI;

/// <summary>
/// Interface for Google Gemini client - model discovery
/// </summary>
public interface IGeminiClient
{
    Task<GeminiModelsResponse> GetModelsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Response from Gemini GET /v1beta/models endpoint
/// </summary>
public class GeminiModelsResponse
{
    [JsonPropertyName("models")]
    public List<GeminiModel> Models { get; set; } = new();

    [JsonPropertyName("nextPageToken")]
    public string? NextPageToken { get; set; }
}

/// <summary>
/// Individual model from Gemini API
/// </summary>
public class GeminiModel
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("supportedGenerationMethods")]
    public List<string> SupportedGenerationMethods { get; set; } = new();

    [JsonPropertyName("inputTokenLimit")]
    public int? InputTokenLimit { get; set; }

    [JsonPropertyName("outputTokenLimit")]
    public int? OutputTokenLimit { get; set; }
}
