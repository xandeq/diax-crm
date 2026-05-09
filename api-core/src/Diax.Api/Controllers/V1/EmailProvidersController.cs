using Asp.Versioning;
using Diax.Api.Auth;
using Diax.Application.EmailMarketing;
using Diax.Application.EmailMarketing.Pro;
using Diax.Application.EmailMarketing.Pro.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[Authorize]
[RequirePermission("campaigns.manage")]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/email-providers")]
[Produces("application/json")]
public class EmailProvidersController : BaseApiController
{
    private readonly IProviderHealthService _healthService;
    private readonly ISmartPreselectionService _preselectService;
    private readonly EmailMarketingService _emailMarketingService;

    public EmailProvidersController(
        IProviderHealthService healthService,
        ISmartPreselectionService preselectService,
        EmailMarketingService emailMarketingService)
    {
        _healthService = healthService;
        _preselectService = preselectService;
        _emailMarketingService = emailMarketingService;
    }

    /// <summary>
    /// Returns send counts, limits and health status for each email provider.
    /// </summary>
    [HttpGet("health")]
    public async Task<IActionResult> GetHealth(CancellationToken cancellationToken)
    {
        var result = await _healthService.GetHealthAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Returns up to maxPerProvider * 3 leads pre-assigned to Brevo/Mailjet/Resend
    /// sorted by segment (Hot first), score and cooldown recency.
    /// </summary>
    [HttpPost("smart-preselect")]
    public async Task<IActionResult> SmartPreselect(
        [FromBody] SmartPreselectRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _preselectService.PreselecAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Queues email items for an existing campaign using the provider assignment
    /// from the smart preselection step (each lead already has AssignedProvider set).
    /// </summary>
    [HttpPost("queue-with-assignment")]
    public async Task<IActionResult> QueueWithAssignment(
        [FromBody] QueueWithAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _emailMarketingService.QueueWithSmartAssignmentAsync(request, cancellationToken);
        return HandleResult(result);
    }
}
