using System.Security.Cryptography;
using System.Text;
using Asp.Versioning;
using Diax.Application.Integrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Diax.Api.Controllers.V1;

/// <summary>
/// Server-to-server integration endpoints. Authenticated via X-Integration-Key header (no JWT).
/// </summary>
[AllowAnonymous]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/integrations")]
[Produces("application/json")]
public class IntegrationsController : BaseApiController
{
    private readonly ICashFlowProjectionIntegrationService _cashFlowService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IntegrationsController> _logger;

    public IntegrationsController(
        ICashFlowProjectionIntegrationService cashFlowService,
        IConfiguration configuration,
        ILogger<IntegrationsController> logger)
    {
        _cashFlowService = cashFlowService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Returns daily cash-flow projection plus availableToInvest and the next big outflow.
    /// Consumed by InvestIQ to power the Cash Parking Advisor.
    /// </summary>
    [HttpGet("cash-flow-projection")]
    public async Task<IActionResult> GetCashFlowProjection(
        [FromHeader(Name = "X-Integration-Key")] string? integrationKey,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var configuredKey = _configuration["Integrations:InvestIQKey"];
        var configuredUserId = _configuration["Integrations:DefaultUserId"];

        if (string.IsNullOrWhiteSpace(configuredKey) || string.IsNullOrWhiteSpace(configuredUserId))
        {
            _logger.LogError("Integrations not configured (InvestIQKey or DefaultUserId missing)");
            return StatusCode(503, new { error = "Integrations.NotConfigured", message = "Integration is not configured on the server" });
        }

        if (string.IsNullOrWhiteSpace(integrationKey) || !TimingSafeEquals(integrationKey, configuredKey))
        {
            _logger.LogWarning("Integrations cash-flow-projection: invalid or missing X-Integration-Key");
            return Unauthorized(new { error = "Integrations.Unauthorized", message = "Invalid integration key" });
        }

        if (!Guid.TryParse(configuredUserId, out var userId))
        {
            _logger.LogError("Integrations:DefaultUserId is not a valid GUID: {Value}", configuredUserId);
            return StatusCode(503, new { error = "Integrations.NotConfigured", message = "Integration default user is not configured correctly" });
        }

        var today = DateTime.UtcNow.Date;
        var from = fromDate?.Date ?? today;
        var to = toDate?.Date ?? today.AddDays(60);

        var result = await _cashFlowService.GetProjectionAsync(userId, from, to, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error.Code, message = result.Error.Message });
    }

    private static bool TimingSafeEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(a),
            Encoding.UTF8.GetBytes(b));
}
