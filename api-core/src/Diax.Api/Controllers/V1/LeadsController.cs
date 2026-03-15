using Diax.Api.Controllers;
using Diax.Application.Common;
using Diax.Application.Customers;
using Diax.Application.Customers.Dtos;
using Diax.Application.Customers.Services;
using Diax.Domain.Customers.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace Diax.Api.Controllers.V1;

[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/leads")]
public class LeadsController : BaseApiController
{
    private readonly CustomerService _service;
    private readonly IExtractorService _extractorService;

    public LeadsController(CustomerService service, IExtractorService extractorService)
    {
        _service = service;
        _extractorService = extractorService;
    }

    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] CustomerStatus? status = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false,
        [FromQuery] bool? hasEmail = null,
        [FromQuery] bool? hasWhatsApp = null,
        [FromQuery] PersonType? personType = null,
        [FromQuery] LeadSource? source = null,
        [FromQuery] LeadSegment? segment = null,
        [FromQuery] bool? neverEmailed = null,
        [FromQuery] DateTime? createdAfter = null)
    {
        var request = new CustomerListRequest
        {
            Page = page,
            PageSize = pageSize,
            Search = search,
            Status = status,
            OnlyLeads = true,
            SortBy = sortBy,
            SortDescending = sortDescending,
            HasEmail = hasEmail,
            HasWhatsApp = hasWhatsApp,
            PersonType = personType,
            Source = source,
            Segment = segment,
            NeverEmailed = neverEmailed,
            CreatedAfter = createdAfter
        };

        var result = await _service.GetPagedAsync(request);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return HandleResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request)
    {
        var result = await _service.CreateAsync(request);

        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
        }

        return HandleResult(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        return HandleResult(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);
        return HandleResult(result);
    }

    [HttpDelete("bulk")]
    public async Task<IActionResult> BulkDelete([FromBody] BulkDeleteRequest request)
    {
        var result = await _service.BulkDeleteAsync(request.Ids);
        return HandleResult(result);
    }

    [HttpPost("import/apify-url")]
    public async Task<IActionResult> ImportFromApify([FromBody] ApifyImportRequest request, [FromServices] IApifyIntegrationService apifyService)
    {
        if (string.IsNullOrWhiteSpace(request.DatasetUrl))
        {
            return BadRequest(new { Message = "A URL do dataset da Apify é obrigatória." });
        }

        var result = await apifyService.ImportDatasetAsync(request.DatasetUrl, (int)request.Source);
        return HandleResult(result);
    }

    [HttpPost("sanitize-base")]
    public async Task<IActionResult> SanitizeBase([FromBody] BulkSanitizationRequest request)
    {
        var result = await _service.SanitizeBaseAsync(request);
        return HandleResult(result);
    }

    [HttpGet("extrator-config")]
    public async Task<IActionResult> GetExtractorConfig(
        [FromServices] IConfigurationProvider configProvider)
    {
        var result = await configProvider.GetExtractorConfigAsync();

        if (result.IsFailure)
        {
            return BadRequest(new
            {
                message = "Extrator configuration not found",
                detail = result.Error.Message
            });
        }

        var (url, _) = result.Value;

        // ✅ Return only URL; token stays server-side for security
        return Ok(new { url });
    }

    [HttpGet("extrator-leads")]
    public async Task<IActionResult> GetExtractorLeads(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? tag,
        [FromQuery] string? city,
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 100)
    {
        var result = await _extractorService.FetchLeadsAsync(
            search, status, tag, city, page, perPage);

        if (result.IsFailure)
        {
            return BadRequest(new { message = result.Error.Message });
        }

        return Ok(result.Value);
    }
}
