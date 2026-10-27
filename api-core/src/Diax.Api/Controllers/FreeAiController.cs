using System.Text.Json.Serialization;
using Diax.Api.Auth;
using Diax.Application.AI.FreeGateway;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Diax.Api.Controllers;

/// <summary>
/// Gateway de IAs GRATUITAS para aplicações externas (ex.: gerador de prompts num notebook),
/// com fallback automático entre provedores (groq, cerebras, gemini, openrouter, huggingface).
///
/// Formato de entrada/saída = OpenAI chat completions, então qualquer SDK da OpenAI funciona
/// apontando base_url para este servidor — basta trocar a API key pela ProxyApiKey.
///
/// Auth: header X-Api-Key com a ProxyApiKey do servidor (mesmo esquema do <see cref="OpenRouterProxyController"/>).
///
/// Uso no SDK da OpenAI (Python):
///   from openai import OpenAI
///   client = OpenAI(
///       api_key="ignored",
///       base_url="https://api.alexandrequeiroz.com.br/free-ai/v1",
///       default_headers={"X-Api-Key": os.environ["DIAX_PROXY_API_KEY"]},
///   )
///   resp = client.chat.completions.create(
///       model="auto",  # ou "gemini/gemini-2.5-flash" (precisa estar em GET /models)
///       messages=[{"role": "user", "content": "oi"}],
///   )
///   print(resp.choices[0].message.content, resp.model)  # resp.x_diax traz provider/model/attempts
///
/// Ou via curl:
///   curl https://api.alexandrequeiroz.com.br/free-ai/v1/chat/completions \
///     -H "X-Api-Key: $DIAX_PROXY_API_KEY" -H "Content-Type: application/json" \
///     -d '{"model":"auto","messages":[{"role":"user","content":"oi"}]}'
/// </summary>
[ApiController]
[Route("free-ai/v1")]
[Authorize(Policy = ApiKeyAuthenticationOptions.ProxyPolicy)]
[EnableRateLimiting("free-ai")]
public class FreeAiController : ControllerBase
{
    private readonly IFreeAiGatewayService _gateway;
    private readonly ILogger<FreeAiController> _logger;

    public FreeAiController(IFreeAiGatewayService gateway, ILogger<FreeAiController> logger)
    {
        _gateway = gateway;
        _logger = logger;
    }

    [HttpPost("chat/completions")]
    public async Task<IActionResult> ChatCompletions([FromBody] FreeAiChatCompletionRequestDto dto, CancellationToken cancellationToken)
    {
        if (dto.Stream == true)
            return BadRequest(new { error = "stream não suportado." });

        var request = new FreeAiChatRequest
        {
            Messages = (dto.Messages ?? new()).Select(m => new FreeAiChatMessage { Role = m.Role, Content = m.Content }).ToList(),
            Temperature = dto.Temperature,
            MaxTokens = dto.MaxTokens,
            Model = dto.Model,
        };

        FreeAiChatResult result;
        try
        {
            result = await _gateway.CompleteAsync(request, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }

        if (!result.Success)
        {
            _logger.LogWarning("FreeAiController: todos os provedores falharam ({Count} tentativas).", result.Attempts.Count);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                error = "Todos os provedores de IA gratuitos falharam.",
                attempts = result.Attempts,
            });
        }

        var response = new FreeAiChatCompletionResponseDto
        {
            Id = $"free-ai-{Guid.NewGuid():N}",
            Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Model = $"{result.Provider}/{result.Model}",
            Choices = new List<FreeAiChoiceDto>
            {
                new()
                {
                    Index = 0,
                    Message = new FreeAiChatMessageDto { Role = "assistant", Content = result.Content ?? string.Empty },
                    FinishReason = result.FinishReason ?? "stop",
                },
            },
            Usage = new FreeAiUsageDto
            {
                PromptTokens = result.PromptTokens ?? 0,
                CompletionTokens = result.CompletionTokens ?? 0,
                TotalTokens = (result.PromptTokens ?? 0) + (result.CompletionTokens ?? 0),
            },
            XDiax = new FreeAiExtraDto
            {
                Provider = result.Provider ?? string.Empty,
                Model = result.Model ?? string.Empty,
                Attempts = result.Attempts,
            },
        };

        return Ok(response);
    }

    [HttpGet("models")]
    public IActionResult GetModels()
    {
        var chain = _gateway.GetChain();
        var data = new List<object> { new { id = "auto", @object = "model", owned_by = "diax-free-ai-gateway" } };
        data.AddRange(chain.Select(e => (object)new { id = $"{e.Provider}/{e.Model}", @object = "model", owned_by = e.Provider }));

        return Ok(new { @object = "list", data });
    }

    [HttpGet("health")]
    public async Task<IActionResult> GetHealth([FromQuery] bool refresh, CancellationToken cancellationToken)
    {
        var health = await _gateway.CheckHealthAsync(refresh, cancellationToken);
        return Ok(new
        {
            ok = health.OkCount,
            total = health.TotalCount,
            cached = health.Cached,
            checkedAt = health.CheckedAt,
            items = health.Items,
        });
    }
}

public class FreeAiChatMessageDto
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class FreeAiChatCompletionRequestDto
{
    public string? Model { get; set; }
    public List<FreeAiChatMessageDto>? Messages { get; set; }
    public double? Temperature { get; set; }

    [JsonPropertyName("max_tokens")]
    public int? MaxTokens { get; set; }

    public bool? Stream { get; set; }
}

public class FreeAiChatCompletionResponseDto
{
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("object")]
    public string Object { get; set; } = "chat.completion";

    public long Created { get; set; }
    public string Model { get; set; } = string.Empty;
    public List<FreeAiChoiceDto> Choices { get; set; } = new();
    public FreeAiUsageDto Usage { get; set; } = new();

    [JsonPropertyName("x_diax")]
    public FreeAiExtraDto XDiax { get; set; } = new();
}

public class FreeAiChoiceDto
{
    public int Index { get; set; }
    public FreeAiChatMessageDto Message { get; set; } = new();

    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; } = "stop";
}

public class FreeAiUsageDto
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}

public class FreeAiExtraDto
{
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public List<FreeAiAttempt> Attempts { get; set; } = new();
}
