using System.Text.Json.Serialization;

namespace Diax.Application.AI;

/// <summary>
/// Interface for OpenRouter client
/// </summary>
public interface IOpenRouterClient
{
    Task<OpenRouterModelsResponse> GetModelsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Response from OpenRouter /models endpoint
/// </summary>
public class OpenRouterModelsResponse
{
    [JsonPropertyName("data")]
    public List<OpenRouterModel> Data { get; set; } = new();
}

/// <summary>
/// Individual model from OpenRouter API
/// </summary>
public class OpenRouterModel
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("context_length")]
    public int? ContextLength { get; set; }

    [JsonPropertyName("pricing")]
    public OpenRouterPricing? Pricing { get; set; }

    [JsonPropertyName("top_provider")]
    public OpenRouterTopProvider? TopProvider { get; set; }
}

/// <summary>
/// Pricing information from OpenRouter
/// </summary>
public class OpenRouterPricing
{
    [JsonPropertyName("prompt")]
    public string? Prompt { get; set; }

    [JsonPropertyName("completion")]
    public string? Completion { get; set; }
}

/// <summary>
/// Top provider information
/// </summary>
public class OpenRouterTopProvider
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("max_completion_tokens")]
    public int? MaxCompletionTokens { get; set; }
}
