using System.Text.Json.Serialization;
using Asp.Versioning;
using Diax.Application.EmailMarketing.Pro;
using Diax.Domain.EmailMarketing;
using Diax.Domain.EmailMarketing.Enums;
using Diax.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/email-suppressions")]
[Authorize]
public class EmailSuppressionController : BaseApiController
{
    private readonly ISuppressionService _suppressionService;

    public EmailSuppressionController(ISuppressionService suppressionService)
    {
        _suppressionService = suppressionService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var suppressions = await _suppressionService.GetAllAsync(cancellationToken);
        return Ok(suppressions.Select(s => new SuppressionDto(s)));
    }

    [HttpPost]
    public async Task<IActionResult> Add(
        [FromBody] AddSuppressionRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.Domain))
            return BadRequest("Informe um email ou dominio.");

        if (!string.IsNullOrWhiteSpace(request.Email))
            await _suppressionService.SuppressEmailAsync(request.Email, SuppressionReason.ManualOptOut, "manual", cancellationToken);
        else
            await _suppressionService.SuppressDomainAsync(request.Domain!, SuppressionReason.ManualOptOut, "manual", cancellationToken);

        return Ok();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remove(Guid id, CancellationToken cancellationToken)
    {
        await _suppressionService.RemoveAsync(id, cancellationToken);
        return NoContent();
    }
}

public record AddSuppressionRequest(
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("domain")] string? Domain);

public record SuppressionDto(
    Guid Id,
    string? Email,
    string? DomainPattern,
    string Reason,
    string Source,
    DateTime SuppressedAt)
{
    public SuppressionDto(EmailSuppression s) : this(
        s.Id,
        s.Email,
        s.DomainPattern,
        s.Reason.ToString(),
        s.Source,
        s.SuppressedAt)
    { }
}
