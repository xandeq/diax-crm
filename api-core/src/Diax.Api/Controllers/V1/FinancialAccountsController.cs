using Asp.Versioning;
using Diax.Application.Finance;
using Diax.Application.Finance.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public class FinancialAccountsController : BaseApiController
{
    private readonly FinancialAccountService _service;
    private readonly ILogger<FinancialAccountsController> _logger;

    public FinancialAccountsController(FinancialAccountService service, ILogger<FinancialAccountsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        _logger.LogInformation("GET /api/v1/financialaccounts - Request received");
        var result = await _service.GetAllAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            _logger.LogWarning("GET /api/v1/financialaccounts - Failed: {ErrorCode} - {ErrorMessage}",
                result.Error?.Code, result.Error?.Message);
        }
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
    {
        _logger.LogInformation("GET /api/v1/financialaccounts/active - Request received");
        var result = await _service.GetActiveAccountsAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            _logger.LogWarning("GET /api/v1/financialaccounts/active - Failed: {ErrorCode} - {ErrorMessage}",
                result.Error?.Code, result.Error?.Message);
        }
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GET /api/v1/financialaccounts/{Id} - Request received", id);
        var result = await _service.GetByIdAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            _logger.LogWarning("GET /api/v1/financialaccounts/{Id} - Failed: {ErrorCode} - {ErrorMessage}",
                id, result.Error?.Code, result.Error?.Message);
        }
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFinancialAccountRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(request, cancellationToken);
        return result.IsSuccess ? CreatedAtAction(nameof(GetById), new { id = result.Value, version = "1" }, result.Value) : BadRequest(result.Error);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFinancialAccountRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.UpdateAsync(id, request, cancellationToken);
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.DeleteAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }
}
