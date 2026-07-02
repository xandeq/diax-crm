using Asp.Versioning;
using Diax.Api.Auth;
using Diax.Application.EmailMarketing;
using Diax.Application.EmailMarketing.Dispatch;
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
    private readonly IProviderCircuitBreaker _providerBreaker;
    private readonly IPilotCircuitBreaker _pilotBreaker;
    private readonly IEmailProviderPolicy _providerPolicy;

    public EmailProvidersController(
        IProviderHealthService healthService,
        ISmartPreselectionService preselectService,
        EmailMarketingService emailMarketingService,
        IProviderCircuitBreaker providerBreaker,
        IPilotCircuitBreaker pilotBreaker,
        IEmailProviderPolicy providerPolicy)
    {
        _healthService = healthService;
        _preselectService = preselectService;
        _emailMarketingService = emailMarketingService;
        _providerBreaker = providerBreaker;
        _pilotBreaker = pilotBreaker;
        _providerPolicy = providerPolicy;
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

    /// <summary>
    /// Estado dos circuit breakers: global (piloto) + por provider, e a lista de
    /// providers habilitados/desabilitados.
    /// </summary>
    [HttpGet("breaker-status")]
    public IActionResult GetBreakerStatus()
    {
        var providerStates = _providerBreaker.GetStates();

        return Ok(new
        {
            pilot = new
            {
                isOpen = _pilotBreaker.IsOpen,
                reason = _pilotBreaker.Reason,
                currentErrorRate = _pilotBreaker.CurrentErrorRate,
                webhookFailureCount = _pilotBreaker.WebhookFailureCount
            },
            providers = providerStates.Values.OrderBy(s => s.Provider).Select(s => new
            {
                provider = s.Provider,
                isOpen = s.IsOpen,
                isHalfOpen = s.IsHalfOpen,
                reason = s.Reason,
                openedAtUtc = s.OpenedAtUtc,
                errorRatePercent = s.ErrorRatePercent
            }),
            enabledProviders = _providerPolicy.EnabledProviders.Select(p => p.ToString())
        });
    }

    /// <summary>
    /// Fecha manualmente o breaker de um provider específico do dispatch unificado.
    /// (O breaker global do piloto tem o próprio reset em /email-campaigns/pilot/reset.)
    /// </summary>
    [HttpPost("breaker/reset")]
    [Authorize(Roles = "Admin")]
    public IActionResult ResetProviderBreaker([FromQuery] string provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            return BadRequest(new { error = "Validation", message = "Informe o provider (ex: ?provider=brevo)." });
        }

        _providerBreaker.Reset(provider.Trim().ToLowerInvariant());
        return Ok(new { reset = provider.Trim().ToLowerInvariant() });
    }
}
