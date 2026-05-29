using System.Net;
using System.Text;
using System.Text.Json;
using Diax.Application.AiChat;
using Diax.Infrastructure.AiChat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;

namespace Diax.Tests.Application.AiChat;

/// <summary>
/// Tests for AnthropicChatClient.CompleteWithToolsAsync — verifies stop_reason parsing.
/// Uses a mocked HttpMessageHandler to return canned JSON without real HTTP calls.
/// </summary>
public class AnthropicToolUseTests
{
    // ── helpers ──────────────────────────────────────────────────────────────

    private static AnthropicChatClient BuildClientWithHandler(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.anthropic.com/v1/")
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ANTHROPIC_API_KEY", "test-key" }
            })
            .Build();

        return new AnthropicChatClient(
            httpClient,
            config,
            NullLogger<AnthropicChatClient>.Instance);
    }

    private static Mock<HttpMessageHandler> MockHandler(string responseJson, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = status,
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            });
        return handlerMock;
    }

    private static AnthropicToolDefinition SampleTool()
    {
        var schema = JsonSerializer.Deserialize<JsonElement>("""
            {
                "type": "object",
                "properties": {
                    "lead_id": { "type": "string" },
                    "new_status": { "type": "string" }
                },
                "required": ["lead_id", "new_status"]
            }
            """);
        return new AnthropicToolDefinition(
            "update_lead_status",
            "Updates the status of a lead in the CRM.",
            schema);
    }

    private static AnthropicMessageRequest SampleRequest() =>
        new AnthropicMessageRequest(
            Model: "claude-sonnet-4-5",
            Messages: [new AnthropicMessage("user", "Atualiza o lead X para Qualified")],
            MaxTokens: 1024,
            System: null,
            EnablePromptCache: false);

    // ── TEST 1: stop_reason = "tool_use" returns ToolUseRequest ──────────────

    [Fact]
    public async Task CompleteWithToolsAsync_WhenStopReasonIsToolUse_ReturnsToolUseRequest()
    {
        // Arrange — canned Anthropic response with stop_reason = "tool_use"
        const string cannedJson = """
            {
              "id": "msg_01Aq9w938a90dw8q",
              "type": "message",
              "role": "assistant",
              "stop_reason": "tool_use",
              "model": "claude-sonnet-4-5",
              "content": [
                {
                  "type": "text",
                  "text": "Vou atualizar o status do lead para você."
                },
                {
                  "type": "tool_use",
                  "id": "toolu_X",
                  "name": "update_lead_status",
                  "input": {
                    "lead_id": "abc-123",
                    "new_status": "Qualified"
                  }
                }
              ],
              "usage": {
                "input_tokens": 430,
                "output_tokens": 72,
                "cache_read_input_tokens": 0,
                "cache_creation_input_tokens": 0
              }
            }
            """;

        var handler = MockHandler(cannedJson);
        var client = BuildClientWithHandler(handler.Object);

        // Act
        var result = await client.CompleteWithToolsAsync(
            SampleRequest(),
            [SampleTool()],
            CancellationToken.None);

        // Assert — must be ToolUseRequest
        var toolUse = Assert.IsType<AnthropicToolResult.ToolUseRequest>(result);
        Assert.Equal("toolu_X", toolUse.ToolUseId);
        Assert.Equal("update_lead_status", toolUse.ToolName);
        Assert.Equal("abc-123", toolUse.ToolInput.GetProperty("lead_id").GetString());
        Assert.Equal("Qualified", toolUse.ToolInput.GetProperty("new_status").GetString());
        Assert.Equal(430, toolUse.Usage.InputTokens);
        Assert.Equal(72, toolUse.Usage.OutputTokens);
    }

    // ── TEST 2: stop_reason = "end_turn" returns TextResponse ────────────────

    [Fact]
    public async Task CompleteWithToolsAsync_WhenStopReasonIsEndTurn_ReturnsTextResponse()
    {
        // Arrange — canned Anthropic response with stop_reason = "end_turn"
        const string cannedJson = """
            {
              "id": "msg_02Bx8z123",
              "type": "message",
              "role": "assistant",
              "stop_reason": "end_turn",
              "model": "claude-sonnet-4-5",
              "content": [
                {
                  "type": "text",
                  "text": "Aqui está a resposta em texto."
                }
              ],
              "usage": {
                "input_tokens": 100,
                "output_tokens": 20,
                "cache_read_input_tokens": 0,
                "cache_creation_input_tokens": 0
              }
            }
            """;

        var handler = MockHandler(cannedJson);
        var client = BuildClientWithHandler(handler.Object);

        // Act
        var result = await client.CompleteWithToolsAsync(
            SampleRequest(),
            [SampleTool()],
            CancellationToken.None);

        // Assert — must be TextResponse
        var textResponse = Assert.IsType<AnthropicToolResult.TextResponse>(result);
        Assert.Equal("Aqui está a resposta em texto.", textResponse.Text);
        Assert.Equal(100, textResponse.Usage.InputTokens);
        Assert.Equal(20, textResponse.Usage.OutputTokens);
    }
}
