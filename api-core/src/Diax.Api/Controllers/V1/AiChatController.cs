using System.Text.Json;
using Asp.Versioning;
using Diax.Application.AiChat;
using Diax.Application.AiChat.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Diax.Api.Controllers.V1;

/// <summary>
/// Proxy autenticado da Anthropic Messages API. Backend de /ai-chat no frontend.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ai/[action]")]
[Route("api/ai")] // alias sem versão para compatibilidade com o frontend
[Produces("application/json")]
[Authorize]
[EnableRateLimiting("ai-chat")]
public class AiChatController : BaseApiController
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IAiChatService _service;
    private readonly ILogger<AiChatController> _logger;

    public AiChatController(IAiChatService service, ILogger<AiChatController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // ===== GET /api/ai/conversations =====
    [HttpGet("conversations")]
    public async Task<IActionResult> ListConversations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.ListConversationsAsync(userId.Value, page, pageSize, includeArchived, cancellationToken);
        return HandleResult(result);
    }

    // ===== GET /api/ai/conversations/{id} =====
    [HttpGet("conversations/{id:guid}")]
    public async Task<IActionResult> GetConversation(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.GetConversationAsync(id, userId.Value, cancellationToken);
        return HandleResult(result);
    }

    // ===== POST /api/ai/conversations =====
    [HttpPost("conversations")]
    public async Task<IActionResult> CreateConversation(
        [FromBody] CreateConversationRequest body,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.CreateConversationAsync(userId.Value, body, cancellationToken);
        return HandleResult(result);
    }

    // ===== PATCH /api/ai/conversations/{id} =====
    [HttpPatch("conversations/{id:guid}")]
    public async Task<IActionResult> UpdateConversation(
        Guid id,
        [FromBody] UpdateConversationRequest body,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.UpdateConversationAsync(id, userId.Value, body, cancellationToken);
        return HandleResult(result);
    }

    // ===== DELETE /api/ai/conversations/{id} (soft delete via archive) =====
    [HttpDelete("conversations/{id:guid}")]
    public async Task<IActionResult> ArchiveConversation(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.ArchiveConversationAsync(id, userId.Value, cancellationToken);
        return HandleResult(result);
    }

    // ===== POST /api/ai/chat/stream  (SSE) =====
    [HttpPost("chat/stream")]
    [EnableRateLimiting("ai-chat-stream")]
    public async Task ChatStream(
        [FromBody] ChatRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        // SSE response setup
        Response.StatusCode = StatusCodes.Status200OK;
        Response.Headers["Content-Type"] = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache, no-transform";
        Response.Headers["X-Accel-Buffering"] = "no"; // nginx — desliga buffering
        Response.Headers["Connection"] = "keep-alive";

        // Desabilita buffering do ASP.NET Core (funciona no Kestrel e IIS in-process).
        // Sem isso, o IIS pode acumular chunks antes de enviar ao cliente.
        HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

        try
        {
            await foreach (var chunk in _service.StreamChatAsync(userId.Value, request, cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested) break;

                try
                {
                    var payload = JsonSerializer.Serialize(chunk, JsonOpts);
                    await Response.WriteAsync($"data: {payload}\n\n", cancellationToken);
                    await Response.Body.FlushAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("[AiChat] Stream cancelado pelo cliente");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[AiChat] Falha escrevendo chunk SSE");
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("[AiChat] Stream cancelado (OperationCanceled no foreach)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AiChat] Exceção não tratada no stream: {Message}", ex.Message);
            try
            {
                var errPayload = JsonSerializer.Serialize(
                    new ChatStreamChunkDto(ChatStreamChunkType.Error, Error: $"Erro interno: {ex.Message}"), JsonOpts);
                await Response.WriteAsync($"data: {errPayload}\n\n", CancellationToken.None);
                await Response.Body.FlushAsync(CancellationToken.None);
            }
            catch { /* cliente desconectou */ }
        }

        try
        {
            await Response.WriteAsync("data: [DONE]\n\n", CancellationToken.None);
            await Response.Body.FlushAsync(CancellationToken.None);
        }
        catch { /* cliente já desconectou */ }
    }

    // ===== GET /api/ai/usage =====
    [HttpGet("usage")]
    public async Task<IActionResult> GetUsage(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.GetMonthlyUsageAsync(userId.Value, cancellationToken);
        return HandleResult(result);
    }
}
