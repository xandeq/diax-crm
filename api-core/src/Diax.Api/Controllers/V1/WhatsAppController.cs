using Asp.Versioning;
using Diax.Application.Outreach;
using Diax.Application.Outreach.Dtos;
using Diax.Application.WhatsApp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/whatsapp")]
[Produces("application/json")]
public class WhatsAppController : BaseApiController
{
    private readonly OutreachService _outreachService;
    private readonly ILogger<WhatsAppController> _logger;

    public WhatsAppController(
        OutreachService outreachService,
        ILogger<WhatsAppController> logger)
    {
        _outreachService = outreachService;
        _logger = logger;
    }

    /// <summary>
    /// Obtém o status da conexão WhatsApp.
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(WhatsAppConnectionStatus), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Consultando status da conexão WhatsApp");
        var result = await _outreachService.GetWhatsAppStatusAsync(cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Obtém leads prontos para envio via WhatsApp.
    /// </summary>
    [HttpGet("ready-leads")]
    [ProducesResponseType(typeof(List<WhatsAppReadyLeadResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetReadyLeads(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Buscando leads prontos para WhatsApp");
        var result = await _outreachService.GetWhatsAppReadyLeadsAsync(50, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Envia uma mensagem WhatsApp individual para um cliente.
    /// </summary>
    [HttpPost("send")]
    [ProducesResponseType(typeof(WhatsAppSendResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Send(
        [FromBody] WhatsAppSendRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Enviando mensagem WhatsApp para cliente");
        var result = await _outreachService.SendWhatsAppToCustomerAsync(request, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Envia campanha de WhatsApp em massa.
    /// </summary>
    [HttpPost("send-campaign")]
    [ProducesResponseType(typeof(WhatsAppSendResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SendCampaign(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando envio de campanha WhatsApp em massa");
        var result = await _outreachService.SendWhatsAppCampaignAsync(cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Envia mensagem de follow-up via WhatsApp.
    /// </summary>
    [HttpPost("send-followup")]
    [ProducesResponseType(typeof(WhatsAppSendResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SendFollowUp(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Enviando follow-up via WhatsApp");
        var result = await _outreachService.SendWhatsAppFollowUpAsync(cancellationToken);
        return HandleResult(result);
    }
}
