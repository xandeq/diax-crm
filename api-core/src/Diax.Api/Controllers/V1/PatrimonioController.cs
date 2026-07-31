using Asp.Versioning;
using Diax.Application.Finance.Patrimonio;
using Diax.Application.Finance.Patrimonio.Dtos;
using Diax.Domain.Finance.Assets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/patrimonio")]
[Produces("application/json")]
public class PatrimonioController : BaseApiController
{
    private readonly AssetService _assetService;
    private readonly NetWorthService _netWorthService;
    private readonly ILogger<PatrimonioController> _logger;

    public PatrimonioController(
        AssetService assetService,
        NetWorthService netWorthService,
        ILogger<PatrimonioController> logger)
    {
        _assetService = assetService;
        _netWorthService = netWorthService;
        _logger = logger;
    }

    [HttpGet("assets")]
    public async Task<IActionResult> GetAssets(
        [FromQuery(Name = "class")] AssetClass? assetClass,
        [FromQuery] AssetOwnership? ownership,
        [FromQuery] AssetLiquidity? liquidity,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        _logger.LogInformation("GET /api/v1/patrimonio/assets - Request received");
        var result = await _assetService.GetAllAsync(userId.Value, assetClass, ownership, liquidity, cancellationToken);
        if (!result.IsSuccess)
        {
            _logger.LogError("GET /api/v1/patrimonio/assets - Failed: {ErrorCode} - {ErrorMessage}",
                result.Error?.Code, result.Error?.Message);

            if (result.Error?.Code?.EndsWith("Failed") == true)
            {
                return StatusCode(500, result.Error);
            }
            return BadRequest(result.Error);
        }
        return Ok(result.Value);
    }

    [HttpGet("assets/{id}")]
    public async Task<IActionResult> GetAssetById(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        _logger.LogInformation("GET /api/v1/patrimonio/assets/{Id} - Request received", id);
        var result = await _assetService.GetByIdAsync(id, userId.Value, cancellationToken);
        if (!result.IsSuccess)
        {
            _logger.LogError("GET /api/v1/patrimonio/assets/{Id} - Failed: {ErrorCode} - {ErrorMessage}",
                id, result.Error?.Code, result.Error?.Message);

            if (result.Error?.Code?.EndsWith("Failed") == true)
            {
                return StatusCode(500, result.Error);
            }
            return NotFound(result.Error);
        }
        return Ok(result.Value);
    }

    [HttpPost("assets")]
    public async Task<IActionResult> CreateAsset([FromBody] CreateAssetRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        _logger.LogInformation("POST /api/v1/patrimonio/assets - Request received");
        var result = await _assetService.CreateAsync(request, userId.Value, cancellationToken);
        if (!result.IsSuccess)
        {
            _logger.LogError("POST /api/v1/patrimonio/assets - Failed: {ErrorCode} - {ErrorMessage}",
                result.Error?.Code, result.Error?.Message);

            if (result.Error?.Code?.EndsWith("Failed") == true)
            {
                return StatusCode(500, result.Error);
            }
            return BadRequest(result.Error);
        }
        return CreatedAtAction(nameof(GetAssetById), new { id = result.Value.Id, version = "1" }, result.Value);
    }

    [HttpPut("assets/{id}")]
    public async Task<IActionResult> UpdateAsset(Guid id, [FromBody] UpdateAssetRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        _logger.LogInformation("PUT /api/v1/patrimonio/assets/{Id} - Request received", id);
        var result = await _assetService.UpdateAsync(id, request, userId.Value, cancellationToken);
        if (!result.IsSuccess)
        {
            _logger.LogError("PUT /api/v1/patrimonio/assets/{Id} - Failed: {ErrorCode} - {ErrorMessage}",
                id, result.Error?.Code, result.Error?.Message);

            if (result.Error?.Code?.EndsWith("Failed") == true)
            {
                return StatusCode(500, result.Error);
            }
            return BadRequest(result.Error);
        }
        return NoContent();
    }

    [HttpDelete("assets/{id}")]
    public async Task<IActionResult> DeleteAsset(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        _logger.LogInformation("DELETE /api/v1/patrimonio/assets/{Id} - Request received", id);
        var result = await _assetService.DeleteAsync(id, userId.Value, cancellationToken);
        if (!result.IsSuccess)
        {
            _logger.LogError("DELETE /api/v1/patrimonio/assets/{Id} - Failed: {ErrorCode} - {ErrorMessage}",
                id, result.Error?.Code, result.Error?.Message);

            if (result.Error?.Code?.EndsWith("Failed") == true)
            {
                return StatusCode(500, result.Error);
            }
            return BadRequest(result.Error);
        }
        return NoContent();
    }

    [HttpPost("assets/{id}/valuations")]
    public async Task<IActionResult> AddValuation(Guid id, [FromBody] AddValuationRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        _logger.LogInformation("POST /api/v1/patrimonio/assets/{Id}/valuations - Request received", id);
        var result = await _assetService.AddValuationAsync(id, request, userId.Value, cancellationToken);
        if (!result.IsSuccess)
        {
            _logger.LogError("POST /api/v1/patrimonio/assets/{Id}/valuations - Failed: {ErrorCode} - {ErrorMessage}",
                id, result.Error?.Code, result.Error?.Message);

            if (result.Error?.Code?.EndsWith("Failed") == true)
            {
                return StatusCode(500, result.Error);
            }
            return BadRequest(result.Error);
        }
        return CreatedAtAction(nameof(GetAssetById), new { id, version = "1" }, result.Value);
    }

    [HttpGet("networth")]
    public async Task<IActionResult> GetNetWorth(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        _logger.LogInformation("GET /api/v1/patrimonio/networth - Request received");
        var result = await _netWorthService.GetSummaryAsync(userId.Value, cancellationToken);
        if (!result.IsSuccess)
        {
            _logger.LogError("GET /api/v1/patrimonio/networth - Failed: {ErrorCode} - {ErrorMessage}",
                result.Error?.Code, result.Error?.Message);

            if (result.Error?.Code?.EndsWith("Failed") == true)
            {
                return StatusCode(500, result.Error);
            }
            return BadRequest(result.Error);
        }
        return Ok(result.Value);
    }
}
