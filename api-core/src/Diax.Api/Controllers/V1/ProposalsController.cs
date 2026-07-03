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

    /// <summary>Visão pública da proposta (o cliente abre este link).</summary>
    [AllowAnonymous]
    [HttpGet("public/{token}")]
    public async Task<IActionResult> GetPublic(string token, CancellationToken ct)
        => HandleResult(await _proposalService.GetPublicAsync(token, ct));

    /// <summary>Aceite da proposta pelo cliente (link público).</summary>
    [AllowAnonymous]
    [HttpPost("public/{token}/accept")]
    public async Task<IActionResult> AcceptPublic(string token, CancellationToken ct)
        => HandleResult(await _proposalService.AcceptPublicAsync(token, ct));
}
