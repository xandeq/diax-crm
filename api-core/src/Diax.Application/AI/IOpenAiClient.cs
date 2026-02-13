using System.Text.Json.Serialization;

namespace Diax.Application.AI;

/// <summary>
/// Interface for OpenAI client - model discovery
/// </summary>
public interface IOpenAiClient
{

    Task<OpenAiModelsResponse> GetModelsAsync(CancellationToken cancellationToken = default);
    Task<OpenAiChatCompletionResponse> CreateChatCompletionAsync(OpenAiChatCompletionRequest request, CancellationToken cancellationToken = default);
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

public class OpenAiChatCompletionRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<OpenAiMessage> Messages { get; set; } = new();

    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }

    [JsonPropertyName("response_format")]
    public OpenAiResponseFormat? ResponseFormat { get; set; }
}

public class OpenAiMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

public class OpenAiResponseFormat
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";
}

public class OpenAiChatCompletionResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("choices")]
    public List<OpenAiChoice> Choices { get; set; } = new();
}

public class OpenAiChoice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("message")]
    public OpenAiMessage Message { get; set; } = new();

    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; } = string.Empty;
}
