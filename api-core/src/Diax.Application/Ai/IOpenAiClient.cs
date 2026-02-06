using System.Text.Json.Serialization;

namespace Diax.Application.AI;

/// <summary>
/// Interface for OpenAI client - model discovery
/// </summary>
public interface IOpenAiClient
{
    Task<OpenAiModelsResponse> GetModelsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Response from OpenAI GET /v1/models endpoint
/// </summary>
public class OpenAiModelsResponse
{
    [JsonPropertyName("object")]
    public string Object { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public List<OpenAiModel> Data { get; set; } = new();
}

/// <summary>
/// Individual model from OpenAI API
/// </summary>
public class OpenAiModel
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("object")]
    public string Object { get; set; } = string.Empty;

    [JsonPropertyName("created")]
    public long? Created { get; set; }

    [JsonPropertyName("owned_by")]
    public string? OwnedBy { get; set; }
}
