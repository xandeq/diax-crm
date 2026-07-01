using System.Security.Cryptography;
using System.Text;
using Asp.Versioning;
using Diax.Application.Briefings;
using Diax.Application.Briefings.Dtos;
using Diax.Application.EmailMarketing;
using Diax.Application.EmailMarketing.Dispatch;
using Diax.Application.Integrations;
using Diax.Domain.Auth;
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
    private readonly IDailyBriefingService _dailyBriefingService;
    private readonly IUserRepository _userRepository;
    private readonly IEmailDispatchService _emailDispatch;
    private readonly IProviderQuotaGuard _quotaGuard;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IntegrationsController> _logger;

    public IntegrationsController(
        ICashFlowProjectionIntegrationService cashFlowService,
        IDailyBriefingService dailyBriefingService,
        IUserRepository userRepository,
        IEmailDispatchService emailDispatch,
        IProviderQuotaGuard quotaGuard,
        IConfiguration configuration,
        ILogger<IntegrationsController> logger)
    {
        _cashFlowService = cashFlowService;
        _dailyBriefingService = dailyBriefingService;
        _userRepository = userRepository;
        _emailDispatch = emailDispatch;
        _quotaGuard = quotaGuard;
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

    /// <summary>
    /// Ingestão de briefing diário. Consumido pelos geradores (routines + workflow local) todo dia.
    /// Faz upsert por (usuário, dia, source) e purga os briefings de dias anteriores.
    /// </summary>
    [HttpPost("daily-briefings")]
    public async Task<IActionResult> IngestDailyBriefing(
        [FromHeader(Name = "X-Integration-Key")] string? integrationKey,
        [FromBody] IngestDailyBriefingRequest request,
        CancellationToken cancellationToken)
    {
        var configuredKey = _configuration["Integrations:DailyBriefingsKey"];

        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            _logger.LogError("Integrations daily-briefings not configured (DailyBriefingsKey missing)");
            return StatusCode(503, new { error = "Integrations.NotConfigured", message = "Integration is not configured on the server" });
        }

        if (string.IsNullOrWhiteSpace(integrationKey) || !TimingSafeEquals(integrationKey, configuredKey))
        {
            _logger.LogWarning("Integrations daily-briefings: invalid or missing X-Integration-Key");
            return Unauthorized(new { error = "Integrations.Unauthorized", message = "Invalid integration key" });
        }

        if (request is null)
            return BadRequest(new { error = "Integrations.Validation", message = "Body é obrigatório" });

        // Resolve o usuário-dono: Integrations:DefaultUserId (se setado) ou o admin (Auth:AdminEmail).
        var userId = await ResolveOwnerUserIdAsync(cancellationToken);
        if (userId is null)
        {
            _logger.LogError("Integrations daily-briefings: não foi possível resolver o usuário (DefaultUserId/AdminEmail)");
            return StatusCode(503, new { error = "Integrations.NotConfigured", message = "Integration default user is not configured" });
        }

        var result = await _dailyBriefingService.UpsertAsync(userId.Value, request, cancellationToken);
        return result.IsSuccess
            ? Ok(new { id = result.Value })
            : BadRequest(new { error = result.Error.Code, message = result.Error.Message });
    }

    /// <summary>
    /// Endpoint unificado de envio de email com fallback multi-provider.
    /// allowUnaligned=false (padrão): apenas Tier 1 (ESPs com DKIM alinhado).
    /// allowUnaligned=true: esgota Tier 1 → tenta Tier 2 (SMTP próprio) e registra em log.
    /// Campanhas de marketing DEVEM usar allowUnaligned=false.
    /// </summary>
    [HttpPost("send-email")]
    public async Task<IActionResult> SendEmail(
        [FromHeader(Name = "X-Integration-Key")] string? integrationKey,
        [FromBody] SendEmailIntegrationRequest? request,
        CancellationToken ct)
    {
        var configuredKey = _configuration["Integrations:SendEmailKey"];

        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            _logger.LogError("Integrations:SendEmailKey não configurada");
            return StatusCode(503, new { error = "Integrations.NotConfigured", message = "send-email integration not configured" });
        }

        if (string.IsNullOrWhiteSpace(integrationKey) || !TimingSafeEquals(integrationKey, configuredKey))
        {
            _logger.LogWarning("send-email: X-Integration-Key inválida ou ausente");
            return Unauthorized(new { error = "Integrations.Unauthorized", message = "Invalid integration key" });
        }

        if (request is null)
            return BadRequest(new { error = "Integrations.Validation", message = "Body é obrigatório" });

        if (string.IsNullOrWhiteSpace(request.FromEmail))
            return BadRequest(new { error = "Integrations.Validation", message = "fromEmail é obrigatório" });

        if (request.To is null || request.To.Count == 0)
            return BadRequest(new { error = "Integrations.Validation", message = "to deve conter pelo menos um destinatário" });

        if (string.IsNullOrWhiteSpace(request.Html))
            return BadRequest(new { error = "Integrations.Validation", message = "html é obrigatório" });

        var (renderedHtml, renderedText, renderedSubject) = TemplateRenderer.RenderAll(
            request.Html,
            request.Text,
            request.Subject ?? string.Empty,
            request.Variables);

        var message = new EmailMessage
        {
            From = new EmailAddress(request.FromEmail, request.FromName),
            To = request.To.Select(r => new EmailAddress(r.Address, r.Display)).ToList(),
            Cc = request.Cc?.Select(r => new EmailAddress(r.Address, r.Display)).ToList(),
            Bcc = request.Bcc?.Select(r => new EmailAddress(r.Address, r.Display)).ToList(),
            ReplyTo = request.ReplyToEmail is not null ? new EmailAddress(request.ReplyToEmail) : null,
            Subject = renderedSubject,
            Html = renderedHtml,
            Text = renderedText,
            Tags = request.Tags
        };

        var dispatchRequest = new EmailDispatchRequest(
            message,
            request.IdempotencyKey,
            request.ProviderHint,
            Guid.NewGuid().ToString("N"),
            request.AllowUnaligned);

        var result = await _emailDispatch.DispatchAsync(dispatchRequest, ct);

        return result.Status switch
        {
            EmailDispatchStatus.Sent => Ok(new
            {
                status = "Sent",
                messageId = result.MessageId,
                provider = result.ProviderUsed,
                allowUnaligned = result.AllowUnaligned,
                attempts = result.Attempts.Count
            }),
            EmailDispatchStatus.Duplicate => Ok(new
            {
                status = "Duplicate",
                messageId = result.MessageId
            }),
            EmailDispatchStatus.InProgress => StatusCode(409, new
            {
                error = "Dispatch.InProgress",
                message = "A dispatch with this idempotency key is already in progress"
            }),
            EmailDispatchStatus.Rejected => BadRequest(new
            {
                error = "Dispatch.Rejected",
                message = "From domain not configured or not allowed in EmailChain"
            }),
            EmailDispatchStatus.Uncertain => StatusCode(502, new
            {
                error = "Dispatch.Uncertain",
                message = "Send outcome uncertain (hard timeout mid-send) — retry with the SAME idempotency key to avoid duplicates",
                attempts = result.Attempts.Count
            }),
            _ => StatusCode(502, new
            {
                error = "Dispatch.AllFailed",
                message = "All providers failed",
                attempts = result.Attempts.Count
            })
        };
    }

    /// <summary>
    /// Status de quota diária de cada provider de email.
    /// Mostra quantos envios foram usados e quanto resta até meia-noite UTC.
    /// </summary>
    [HttpGet("email-quota")]
    public async Task<IActionResult> GetEmailQuota(
        [FromHeader(Name = "X-Integration-Key")] string? integrationKey,
        CancellationToken ct)
    {
        var configuredKey = _configuration["Integrations:SendEmailKey"];
        if (string.IsNullOrWhiteSpace(configuredKey))
            return StatusCode(503, new { error = "Integrations.NotConfigured" });

        if (string.IsNullOrWhiteSpace(integrationKey) || !TimingSafeEquals(integrationKey, configuredKey))
            return Unauthorized(new { error = "Integrations.Unauthorized" });

        var status = await _quotaGuard.GetStatusAsync(ct);
        var first = status.Values.FirstOrDefault();

        return Ok(new
        {
            dailyResetAtUtc  = first?.DailyResetAtUtc  ?? DateTime.UtcNow.Date.AddDays(1),
            weeklyResetAtUtc = first?.WeeklyResetAtUtc ?? DateTime.UtcNow.Date.AddDays(7 - (int)DateTime.UtcNow.DayOfWeek + 1),
            providers = status.Values.OrderBy(s => s.Provider).Select(s => new
            {
                provider        = s.Provider,
                dailyUsed       = s.DailyUsed,
                dailyLimit      = s.DailyLimit,
                dailyRemaining  = s.DailyRemaining,
                weeklyUsed      = s.WeeklyUsed,
                weeklyLimit     = s.WeeklyLimit,
                weeklyRemaining = s.WeeklyRemaining,
                exhausted       = s.DailyRemaining == 0 || s.WeeklyRemaining == 0
            })
        });
    }

    private async Task<Guid?> ResolveOwnerUserIdAsync(CancellationToken ct)
    {
        var configuredUserId = _configuration["Integrations:DefaultUserId"];
        if (Guid.TryParse(configuredUserId, out var uid))
            return uid;

        var adminEmail = _configuration["Auth:AdminEmail"];
        if (string.IsNullOrWhiteSpace(adminEmail))
            return null;

        var user = await _userRepository.GetByEmailAsync(adminEmail, ct);
        return user?.Id;
    }

    private static bool TimingSafeEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(a),
            Encoding.UTF8.GetBytes(b));
}

// ===== DTOs =====

public sealed class SendEmailIntegrationRequest
{
    public required string FromEmail { get; init; }
    public string? FromName { get; init; }
    public required List<SendEmailRecipient> To { get; init; }
    public List<SendEmailRecipient>? Cc { get; init; }
    public List<SendEmailRecipient>? Bcc { get; init; }
    public string? ReplyToEmail { get; init; }
    public string? Subject { get; init; }
    public required string Html { get; init; }
    public string? Text { get; init; }
    public List<string>? Tags { get; init; }
    public string? IdempotencyKey { get; init; }
    public string? ProviderHint { get; init; }

    /// <summary>
    /// Variáveis para substituição em templates: {"FirstName":"João","Company":"ACME"}.
    /// Tokens no formato {{NomeDaVariável}} em subject, html e text são substituídos.
    /// </summary>
    public Dictionary<string, string>? Variables { get; init; }

    /// <summary>
    /// false (padrão) = campanhas de marketing — nunca usa Tier 2 SMTP.
    /// true = briefings transacionais — pode usar SMTP próprio como fallback.
    /// </summary>
    public bool AllowUnaligned { get; init; } = false;
}

public sealed class SendEmailRecipient
{
    public required string Address { get; init; }
    public string? Display { get; init; }
}
