using Asp.Versioning;
using Diax.Application.Outreach;
using Diax.Application.Outreach.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/outreach")]
[Produces("application/json")]
public class OutreachController : BaseApiController
{
    private readonly OutreachService _outreachService;

    public OutreachController(OutreachService outreachService)
    {
        _outreachService = outreachService;
    }

    /// <summary>
    /// Obtém ou cria a configuração de outreach do usuário.
    /// </summary>
    [HttpGet("config")]
    public async Task<IActionResult> GetConfig(CancellationToken cancellationToken)
    {
        var result = await _outreachService.GetOrCreateConfigAsync(cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Atualiza a configuração de outreach.
    /// </summary>
    [HttpPut("config")]
    public async Task<IActionResult> UpdateConfig(
        [FromBody] UpdateOutreachConfigRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _outreachService.UpdateConfigAsync(request, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Segmenta leads para outreach com base nas regras configuradas.
    /// </summary>
    [HttpPost("segment")]
    public async Task<IActionResult> SegmentLeads(CancellationToken cancellationToken)
    {
        var result = await _outreachService.SegmentLeadsAsync(cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Cria e enfileira uma campanha de outreach para envio.
    /// </summary>
    [HttpPost("send")]
    public async Task<IActionResult> Send(CancellationToken cancellationToken)
    {
        var result = await _outreachService.CreateAndQueueCampaignAsync(cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Retorna o dashboard consolidado de outreach.
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        var result = await _outreachService.GetDashboardAsync(cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Retorna leads prontos para receber outreach.
    /// </summary>
    [HttpGet("ready-leads")]
    public async Task<IActionResult> GetReadyLeads(
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _outreachService.GetReadyLeadsAsync(limit, cancellationToken);
        return HandleResult(result);
    }
}
