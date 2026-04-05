using Asp.Versioning;
using Diax.Application.Finance;
using Diax.Application.Finance.Dtos;
using Diax.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Route("api/v{version:apiVersion}/financial-accounts")]
[Produces("application/json")]
public class FinancialAccountsController : BaseApiController
{
    private readonly FinancialAccountService _service;
    private readonly DiaxDbContext _db;
    private readonly ILogger<FinancialAccountsController> _logger;

    public FinancialAccountsController(FinancialAccountService service, DiaxDbContext db, ILogger<FinancialAccountsController> logger)
    {
        _service = service;
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue) return Unauthorized();

        _logger.LogInformation("GET /api/v1/financialaccounts - Request received");
        var result = await _service.GetAllAsync(userId.Value, cancellationToken);
        if (!result.IsSuccess)
        {
            _logger.LogError("GET /api/v1/financialaccounts - Failed: {ErrorCode} - {ErrorMessage}",
                result.Error?.Code, result.Error?.Message);

            if (result.Error?.Code?.EndsWith("Failed") == true)
            {
                return StatusCode(500, result.Error);
            }
            return BadRequest(result.Error);
        }
        return Ok(result.Value);
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue) return Unauthorized();

        _logger.LogInformation("GET /api/v1/financialaccounts/active - Request received");
        var result = await _service.GetActiveAccountsAsync(userId.Value, cancellationToken);
        if (!result.IsSuccess)
        {
            _logger.LogError("GET /api/v1/financialaccounts/active - Failed: {ErrorCode} - {ErrorMessage}",
                result.Error?.Code, result.Error?.Message);

            if (result.Error?.Code?.EndsWith("Failed") == true)
            {
                return StatusCode(500, result.Error);
            }
            return BadRequest(result.Error);
        }
        return Ok(result.Value);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue) return Unauthorized();

        _logger.LogInformation("GET /api/v1/financialaccounts/{Id} - Request received", id);
        var result = await _service.GetByIdAsync(id, userId.Value, cancellationToken);
        if (!result.IsSuccess)
        {
            _logger.LogError("GET /api/v1/financialaccounts/{Id} - Failed: {ErrorCode} - {ErrorMessage}",
                id, result.Error?.Code, result.Error?.Message);

            if (result.Error?.Code?.EndsWith("Failed") == true)
            {
                return StatusCode(500, result.Error);
            }
            return NotFound(result.Error);
        }
        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFinancialAccountRequest request, CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue) return Unauthorized();

        _logger.LogInformation("POST /api/v1/financialaccounts - Request received");
        var result = await _service.CreateAsync(request, userId.Value, cancellationToken);
        if (!result.IsSuccess)
        {
            _logger.LogError("POST /api/v1/financialaccounts - Failed: {ErrorCode} - {ErrorMessage}",
                result.Error?.Code, result.Error?.Message);

            if (result.Error?.Code?.EndsWith("Failed") == true)
            {
                return StatusCode(500, result.Error);
            }
            return BadRequest(result.Error);
        }
        return CreatedAtAction(nameof(GetById), new { id = result.Value, version = "1" }, result.Value);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFinancialAccountRequest request, CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue) return Unauthorized();

        _logger.LogInformation("PUT /api/v1/financialaccounts/{Id} - Request received", id);
        var result = await _service.UpdateAsync(id, request, userId.Value, cancellationToken);
        if (!result.IsSuccess)
        {
            _logger.LogError("PUT /api/v1/financialaccounts/{Id} - Failed: {ErrorCode} - {ErrorMessage}",
                id, result.Error?.Code, result.Error?.Message);

            if (result.Error?.Code?.EndsWith("Failed") == true)
            {
                return StatusCode(500, result.Error);
            }
            return BadRequest(result.Error);
        }
        return NoContent();
    }

    [HttpPatch("{id}/balance")]
    public async Task<IActionResult> UpdateBalance(Guid id, [FromBody] UpdateBalanceRequest request, CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.UpdateBalanceAsync(id, request.Balance, userId.Value, cancellationToken);
        if (!result.IsSuccess)
            return result.Error?.Code?.EndsWith("Failed") == true ? StatusCode(500, result.Error) : BadRequest(result.Error);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue) return Unauthorized();

        _logger.LogInformation("DELETE /api/v1/financialaccounts/{Id} - Request received", id);
        var result = await _service.DeleteAsync(id, userId.Value, cancellationToken);
        if (!result.IsSuccess)
        {
            _logger.LogError("DELETE /api/v1/financialaccounts/{Id} - Failed: {ErrorCode} - {ErrorMessage}",
                id, result.Error?.Code, result.Error?.Message);

            if (result.Error?.Code?.EndsWith("Failed") == true)
            {
                return StatusCode(500, result.Error);
            }
            return BadRequest(result.Error);
        }
        return NoContent();
    }
}

public record UpdateBalanceRequest(decimal Balance);
