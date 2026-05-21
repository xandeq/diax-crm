namespace Diax.Application.AiChat;

/// <summary>
/// Cliente HTTP raw da Anthropic Messages API com suporte a streaming SSE.
/// Não usa SDK oficial — implementação manual sobre HttpClient.
/// Doc: https://docs.claude.com/en/api/messages-streaming
/// </summary>
public interface IAnthropicChatClient
{
    /// <summary>
    /// Chama a Messages API em modo streaming. Yield de eventos parseados em tempo real.
    /// O chamador é responsável por concatenar deltas de texto e capturar usage no final.
    /// </summary>
    IAsyncEnumerable<AnthropicStreamEvent> StreamMessageAsync(
        AnthropicMessageRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Geração non-streaming curta (usada para gerar título da conversa via Haiku).
    /// </summary>
    Task<AnthropicCompletionResult> CompleteAsync(
        AnthropicMessageRequest request,
        CancellationToken cancellationToken = default);
}

// ===== REQUEST =====

public record AnthropicMessageRequest(
    string Model,
    List<AnthropicMessage> Messages,
    int MaxTokens,
    string? System = null,
    bool EnablePromptCache = true,
    double? Temperature = null);

public record AnthropicMessage(string Role, string Content);

// ===== STREAM EVENTS =====

public abstract record AnthropicStreamEvent;

public record AnthropicMessageStartEvent(string MessageId, AnthropicUsage Usage) : AnthropicStreamEvent;

public record AnthropicTextDeltaEvent(string Text) : AnthropicStreamEvent;

public record AnthropicMessageDeltaEvent(string? StopReason, AnthropicUsage? Usage) : AnthropicStreamEvent;

public record AnthropicMessageStopEvent : AnthropicStreamEvent;

public record AnthropicErrorEvent(string ErrorType, string Message) : AnthropicStreamEvent;

public record AnthropicUsage(
    int InputTokens,
    int OutputTokens,
    int CacheReadInputTokens,
    int CacheCreationInputTokens);

// ===== NON-STREAMING RESULT =====

public record AnthropicCompletionResult(string Text, AnthropicUsage Usage);
