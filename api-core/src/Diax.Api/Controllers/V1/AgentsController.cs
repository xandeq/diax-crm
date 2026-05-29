using Asp.Versioning;
using Diax.Application.Agents.Commercial;
using Diax.Application.Agents.Commercial.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Diax.Api.Controllers.V1;

/// <summary>
/// Agentes de IA do DIAX CRM. Primeiro agente: Comercial (qualificação de leads + outreach).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/agents")]
[Route("api/agents")] // alias sem versão, alinhado ao AiChatController
[Produces("application/json")]
[Authorize]
[EnableRateLimiting("ai-chat")]
public class AgentsController : BaseApiController
{
    private readonly ICommercialAgentService _commercial;
    private readonly ILogger<AgentsController> _logger;

    public AgentsController(ICommercialAgentService commercial, ILogger<AgentsController> logger)
    {
        _commercial = commercial;
        _logger = logger;
    }

    /// <summary>
    /// Chat com o Agente Comercial. Stateless — envie o histórico no corpo.
    /// </summary>
    [HttpPost("commercial/chat")]
    public async Task<IActionResult> CommercialChat(
        [FromBody] CommercialAgentRequest body,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var result = await _commercial.ChatAsync(userId.Value, body, cancellationToken);
        return HandleResult(result);
    }
}
