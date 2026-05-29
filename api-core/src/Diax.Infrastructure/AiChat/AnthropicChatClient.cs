using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Diax.Application.AiChat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.AiChat;

/// <summary>
/// Cliente HTTP raw da Anthropic Messages API com suporte a streaming SSE.
/// Doc: https://docs.claude.com/en/api/messages-streaming
/// </summary>
public class AnthropicChatClient : IAnthropicChatClient
{
    private const string ApiBase = "https://api.anthropic.com/v1/";
    private const string AnthropicVersion = "2023-06-01";

    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly ILogger<AnthropicChatClient> _logger;

    public AnthropicChatClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<AnthropicChatClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // 3-level fallback (env > Anthropic:ApiKey > legacy PromptGenerator key)
        _apiKey = configuration["ANTHROPIC_API_KEY"]
            ?? configuration["Anthropic:ApiKey"]
            ?? configuration["PromptGenerator:Anthropic:ApiKey"];

        if (_httpClient.BaseAddress is null)
            _httpClient.BaseAddress = new Uri(ApiBase);

        // Streaming chats podem demorar — timeout via HttpClient.Timeout não é ideal aqui.
        // Setamos InfiniteTimeSpan e respeitamos o CancellationToken do chamador.
        _httpClient.Timeout = Timeout.InfiniteTimeSpan;
    }

    public async IAsyncEnumerable<AnthropicStreamEvent> StreamMessageAsync(
        AnthropicMessageRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogError("[AnthropicChat] API key não configurada");
            yield return new AnthropicErrorEvent("configuration_error", "Anthropic API key não configurada no servidor.");
            yield break;
        }

        using var httpRequest = BuildRequest(request, stream: true);

        HttpResponseMessage? response = null;
        string? networkError = null;
        try
        {
            response = await _httpClient.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            response?.Dispose();
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AnthropicChat] Falha no SendAsync");
            response?.Dispose();
            response = null;
            networkError = $"Falha ao contactar Anthropic: {ex.Message}";
        }

        if (networkError is not null)
        {
            yield return new AnthropicErrorEvent("network_error", networkError);
            yield break;
        }

        if (response is null)
        {
            yield return new AnthropicErrorEvent("network_error", "Resposta nula da Anthropic.");
            yield break;
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("[AnthropicChat] HTTP {Status}: {Body}", response.StatusCode, errorBody);
            response.Dispose();
            yield return new AnthropicErrorEvent(
                $"http_{(int)response.StatusCode}",
                $"Anthropic API retornou {(int)response.StatusCode}.");
            yield break;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        string? currentEvent = null;
        var dataBuffer = new StringBuilder();

        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string? line;
            try
            {
                line = await reader.ReadLineAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                response.Dispose();
                throw;
            }

            if (line is null) break;

            if (line.Length == 0)
            {
                // Fim de um evento SSE — processa o que está no buffer
                if (currentEvent is not null && dataBuffer.Length > 0)
                {
                    var parsed = ParseEvent(currentEvent, dataBuffer.ToString());
                    if (parsed is not null) yield return parsed;
                }
                currentEvent = null;
                dataBuffer.Clear();
                continue;
            }

            if (line.StartsWith("event:", StringComparison.Ordinal))
            {
                currentEvent = line["event:".Length..].Trim();
            }
            else if (line.StartsWith("data:", StringComparison.Ordinal))
            {
                if (dataBuffer.Length > 0) dataBuffer.Append('\n');
                dataBuffer.Append(line["data:".Length..].TrimStart());
            }
            // Comentários SSE começam com ':' — ignoramos
        }

        // Flush evento final (se servidor não enviar linha vazia ao fim)
        if (currentEvent is not null && dataBuffer.Length > 0)
        {
            var parsed = ParseEvent(currentEvent, dataBuffer.ToString());
            if (parsed is not null) yield return parsed;
        }

        response.Dispose();
    }

    public async Task<AnthropicCompletionResult> CompleteAsync(
        AnthropicMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new InvalidOperationException("Anthropic API key não configurada no servidor.");

        using var httpRequest = BuildRequest(request, stream: false, tools: null);
        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("[AnthropicChat] CompleteAsync HTTP {Status}: {Body}", response.StatusCode, errorBody);
            throw new HttpRequestException($"Anthropic API retornou {(int)response.StatusCode}.");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var root = JsonNode.Parse(json) ?? throw new InvalidOperationException("Resposta JSON inválida.");

        var textBuilder = new StringBuilder();
        if (root["content"] is JsonArray contentArr)
        {
            foreach (var block in contentArr)
            {
                if (block is null) continue;
                if (block["type"]?.GetValue<string>() == "text")
                    textBuilder.Append(block["text"]?.GetValue<string>() ?? string.Empty);
            }
        }

        var usage = ParseUsage(root["usage"]);
        return new AnthropicCompletionResult(textBuilder.ToString(), usage);
    }

    public async Task<AnthropicToolResult> CompleteWithToolsAsync(
        AnthropicMessageRequest request,
        IReadOnlyList<AnthropicToolDefinition> tools,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new InvalidOperationException("Anthropic API key não configurada no servidor.");

        using var httpRequest = BuildRequest(request, stream: false, tools: tools);
        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("[AnthropicChat] CompleteWithToolsAsync HTTP {Status}: {Body}", response.StatusCode, errorBody);
            throw new HttpRequestException($"Anthropic API retornou {(int)response.StatusCode}.");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var root = JsonNode.Parse(json) ?? throw new InvalidOperationException("Resposta JSON inválida.");

        var stopReason = root["stop_reason"]?.GetValue<string>();
        var usage = ParseUsage(root["usage"]);

        if (stopReason == "tool_use")
        {
            var contentArr = root["content"]!.AsArray();
            foreach (var block in contentArr)
            {
                if (block?["type"]?.GetValue<string>() == "tool_use")
                {
                    var toolUseId = block["id"]!.GetValue<string>();
                    var toolName = block["name"]!.GetValue<string>();
                    var toolInput = JsonSerializer.Deserialize<JsonElement>(
                        block["input"]!.ToJsonString());
                    return new AnthropicToolResult.ToolUseRequest(toolUseId, toolName, toolInput, usage);
                }
            }
            // Fallthrough: tool_use stop_reason but no tool_use block found — treat as text
        }

        // end_turn (or unknown stop_reason) — extract all text blocks
        var textBuilder = new StringBuilder();
        if (root["content"] is JsonArray arr)
        {
            foreach (var block in arr)
            {
                if (block?["type"]?.GetValue<string>() == "text")
                    textBuilder.Append(block["text"]?.GetValue<string>() ?? string.Empty);
            }
        }

        return new AnthropicToolResult.TextResponse(textBuilder.ToString(), usage);
    }

    // ===== HELPERS =====

    private HttpRequestMessage BuildRequest(
        AnthropicMessageRequest req,
        bool stream,
        IReadOnlyList<AnthropicToolDefinition>? tools = null)
    {
        var body = BuildRequestBody(req, stream, tools);
        var httpReq = new HttpRequestMessage(HttpMethod.Post, "messages")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        httpReq.Headers.Add("x-api-key", _apiKey);
        httpReq.Headers.Add("anthropic-version", AnthropicVersion);
        if (stream)
            httpReq.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        return httpReq;
    }

    private static string BuildRequestBody(
        AnthropicMessageRequest req,
        bool stream,
        IReadOnlyList<AnthropicToolDefinition>? tools = null)
    {
        var root = new JsonObject
        {
            ["model"] = req.Model,
            ["max_tokens"] = req.MaxTokens,
            ["stream"] = stream
        };

        if (req.Temperature.HasValue)
            root["temperature"] = req.Temperature.Value;

        // System prompt — array de blocks p/ poder anexar cache_control
        if (!string.IsNullOrWhiteSpace(req.System))
        {
            var systemBlock = new JsonObject
            {
                ["type"] = "text",
                ["text"] = req.System
            };
            if (req.EnablePromptCache)
            {
                systemBlock["cache_control"] = new JsonObject { ["type"] = "ephemeral" };
            }
            root["system"] = new JsonArray { systemBlock };
        }

        var messages = new JsonArray();
        // Cache_control no último user message tbm reduz custo em conversas longas.
        var lastUserIndex = -1;
        for (int i = req.Messages.Count - 1; i >= 0; i--)
        {
            if (string.Equals(req.Messages[i].Role, "user", StringComparison.OrdinalIgnoreCase))
            {
                lastUserIndex = i;
                break;
            }
        }

        for (int i = 0; i < req.Messages.Count; i++)
        {
            var m = req.Messages[i];
            var contentBlock = new JsonObject
            {
                ["type"] = "text",
                ["text"] = m.Content
            };
            // Cacheia o histórico até a penúltima user message (reduz cost em conversas longas).
            if (req.EnablePromptCache && i == lastUserIndex - 1 && i >= 0)
            {
                contentBlock["cache_control"] = new JsonObject { ["type"] = "ephemeral" };
            }

            messages.Add(new JsonObject
            {
                ["role"] = m.Role.ToLowerInvariant(),
                ["content"] = new JsonArray { contentBlock }
            });
        }
        root["messages"] = messages;

        // Tool definitions — only added when tools are provided (preserves no-tools body exactly)
        if (tools is { Count: > 0 })
        {
            var toolsArray = new JsonArray();
            foreach (var t in tools)
            {
                toolsArray.Add(new JsonObject
                {
                    ["name"] = t.Name,
                    ["description"] = t.Description,
                    ["input_schema"] = JsonNode.Parse(t.InputSchema.GetRawText())
                });
            }
            root["tools"] = toolsArray;
        }

        return root.ToJsonString();
    }

    private static AnthropicStreamEvent? ParseEvent(string eventName, string data)
    {
        // Eventos relevantes — outros (ping, content_block_start, content_block_stop) ignoramos.
        try
        {
            var node = JsonNode.Parse(data);
            if (node is null) return null;

            return eventName switch
            {
                "message_start" => ParseMessageStart(node),
                "content_block_delta" => ParseContentBlockDelta(node),
                "message_delta" => ParseMessageDelta(node),
                "message_stop" => new AnthropicMessageStopEvent(),
                "error" => ParseError(node),
                _ => null
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static AnthropicStreamEvent? ParseMessageStart(JsonNode node)
    {
        var message = node["message"];
        if (message is null) return null;
        var id = message["id"]?.GetValue<string>() ?? string.Empty;
        var usage = ParseUsage(message["usage"]);
        return new AnthropicMessageStartEvent(id, usage);
    }

    private static AnthropicStreamEvent? ParseContentBlockDelta(JsonNode node)
    {
        var delta = node["delta"];
        if (delta is null) return null;
        if (delta["type"]?.GetValue<string>() != "text_delta") return null;
        var text = delta["text"]?.GetValue<string>() ?? string.Empty;
        return new AnthropicTextDeltaEvent(text);
    }

    private static AnthropicStreamEvent? ParseMessageDelta(JsonNode node)
    {
        var delta = node["delta"];
        var stopReason = delta?["stop_reason"]?.GetValue<string>();
        AnthropicUsage? usage = node["usage"] is not null ? ParseUsage(node["usage"]) : null;
        return new AnthropicMessageDeltaEvent(stopReason, usage);
    }

    private static AnthropicStreamEvent? ParseError(JsonNode node)
    {
        var err = node["error"];
        var type = err?["type"]?.GetValue<string>() ?? "unknown";
        var msg = err?["message"]?.GetValue<string>() ?? "Erro desconhecido.";
        return new AnthropicErrorEvent(type, msg);
    }

    private static AnthropicUsage ParseUsage(JsonNode? usageNode)
    {
        if (usageNode is null) return new AnthropicUsage(0, 0, 0, 0);
        return new AnthropicUsage(
            usageNode["input_tokens"]?.GetValue<int>() ?? 0,
            usageNode["output_tokens"]?.GetValue<int>() ?? 0,
            usageNode["cache_read_input_tokens"]?.GetValue<int>() ?? 0,
            usageNode["cache_creation_input_tokens"]?.GetValue<int>() ?? 0);
    }
}
