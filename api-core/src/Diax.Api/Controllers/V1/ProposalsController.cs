using Asp.Versioning;
using Diax.Application.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

/// <summary>
/// Propostas comerciais: criação/gestão (autenticado) e visualização/aceite
/// pelo link público (anônimo, por token não-enumerável).
/// </summary>
[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/proposals")]
[Produces("application/json")]
public class ProposalsController : BaseApiController
{
    private readonly ProposalService _proposalService;

    public ProposalsController(ProposalService proposalService)
    {
        _proposalService = proposalService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProposalRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { Message = "Usuário não autenticado." });
        return HandleResult(await _proposalService.CreateAsync(request, userId.Value, ct));
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { Message = "Usuário não autenticado." });
        return Ok(await _proposalService.ListAsync(userId.Value, ct));
    }

    [HttpPost("{id:guid}/mark-paid")]
    public async Task<IActionResult> MarkPaid(Guid id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { Message = "Usuário não autenticado." });
        return HandleResult(await _proposalService.MarkPaidAsync(id, userId.Value, ct));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { Message = "Usuário não autenticado." });
        return HandleResult(await _proposalService.CancelAsync(id, userId.Value, ct));
    }

    public record SendProposalEmailRequest(string? PublicBaseUrl);

    /// <summary>Envia a proposta por email ao cliente (fallback multi-provider, idempotente por dia).</summary>
    [HttpPost("{id:guid}/send-email")]
    public async Task<IActionResult> SendEmail(
        Guid id, [FromBody] SendProposalEmailRequest? request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { Message = "Usuário não autenticado." });
        var result = await _proposalService.SendByEmailAsync(id, userId.Value, request?.PublicBaseUrl, ct);
        return result.IsSuccess ? Ok(new { message = result.Value }) : HandleResult(result);
    }

    /// <summary>Visão pública da proposta (o cliente abre este link).</summary>
    [AllowAnonymous]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("public")]
    [HttpGet("public/{token}")]
    public async Task<IActionResult> GetPublic(string token, CancellationToken ct)
    {
        var result = await _proposalService.GetPublicAsync(token, ct);
        return result.IsSuccess ? Ok(AbsolutizeCover(result.Value)) : HandleResult(result);
    }

    /// <summary>Aceite da proposta pelo cliente (link público).</summary>
    [AllowAnonymous]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("public")]
    [HttpPost("public/{token}/accept")]
    public async Task<IActionResult> AcceptPublic(string token, CancellationToken ct)
    {
        var result = await _proposalService.AcceptPublicAsync(token, ct);
        return result.IsSuccess ? Ok(AbsolutizeCover(result.Value)) : HandleResult(result);
    }

    // Capa é salva com URL relativa (/generated-media/...) — o config de base URL não
    // chega em prod via FTP, então absolutizamos pelo request (padrão do módulo de mídia).
    private PublicProposalDto AbsolutizeCover(PublicProposalDto dto)
    {
        if (dto.CoverImageUrl != null && dto.CoverImageUrl.StartsWith('/'))
            dto = dto with { CoverImageUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}{dto.CoverImageUrl}" };
        return dto;
    }
}
