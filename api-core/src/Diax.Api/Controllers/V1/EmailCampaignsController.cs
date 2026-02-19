using Asp.Versioning;
using Diax.Application.EmailMarketing;
using Diax.Application.EmailMarketing.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/email-campaigns")]
[Produces("application/json")]
public class EmailCampaignsController : BaseApiController
{
    private readonly EmailMarketingService _emailMarketingService;

    public EmailCampaignsController(EmailMarketingService emailMarketingService)
    {
        _emailMarketingService = emailMarketingService;
    }

    [HttpPost("campaigns")]
    public async Task<IActionResult> CreateCampaign(
        [FromBody] CreateEmailCampaignRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _emailMarketingService.CreateCampaignAsync(request, cancellationToken);
        return HandleResult(result);
    }

    [HttpPut("campaigns/{campaignId:guid}")]
    public async Task<IActionResult> UpdateCampaign(
        [FromRoute] Guid campaignId,
        [FromBody] UpdateEmailCampaignRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _emailMarketingService.UpdateCampaignAsync(campaignId, request, cancellationToken);
        return HandleResult(result);
    }

    [HttpPost("campaigns/{campaignId:guid}/schedule")]
    public async Task<IActionResult> ScheduleCampaign(
        [FromRoute] Guid campaignId,
        [FromBody] ScheduleEmailCampaignRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _emailMarketingService.ScheduleCampaignAsync(campaignId, request, cancellationToken);
        return HandleResult(result);
    }

    [HttpPost("campaigns/{campaignId:guid}/queue")]
    public async Task<IActionResult> QueueCampaignRecipients(
        [FromRoute] Guid campaignId,
        [FromBody] QueueCampaignRecipientsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _emailMarketingService.QueueCampaignRecipientsAsync(campaignId, request, cancellationToken);
        return HandleResult(result);
    }

    [HttpPost("campaigns/{campaignId:guid}/preview")]
    public async Task<IActionResult> PreviewCampaign(
        [FromRoute] Guid campaignId,
        [FromBody] PreviewCampaignRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _emailMarketingService.PreviewCampaignAsync(campaignId, request, cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("campaigns")]
    public async Task<IActionResult> GetCampaigns(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _emailMarketingService.GetCampaignsByCurrentUserAsync(page, pageSize, cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("campaigns/{campaignId:guid}")]
    public async Task<IActionResult> GetCampaignById(
        [FromRoute] Guid campaignId,
        CancellationToken cancellationToken = default)
    {
        var result = await _emailMarketingService.GetCampaignByIdAsync(campaignId, cancellationToken);
        return HandleResult(result);
    }

    [HttpPost("send-single")]
    public async Task<IActionResult> QueueSingle(
        [FromBody] QueueSingleEmailRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _emailMarketingService.QueueSingleAsync(request, cancellationToken);
        return HandleResult(result);
    }

    [HttpPost("send-bulk")]
    public async Task<IActionResult> QueueBulk(
        [FromBody] QueueBulkEmailRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _emailMarketingService.QueueBulkForCustomersAsync(request, cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("queue")]
    public async Task<IActionResult> GetQueue(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _emailMarketingService.GetQueueByCurrentUserAsync(page, pageSize, cancellationToken);
        return HandleResult(result);
    }
}
