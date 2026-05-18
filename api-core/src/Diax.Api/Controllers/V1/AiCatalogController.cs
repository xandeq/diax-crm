using Asp.Versioning;
using Diax.Application.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ai/catalog")]
[Authorize]
public class AiCatalogController : BaseApiController
{
    private readonly IAiCatalogService _catalogService;
    private readonly ILogger<AiCatalogController> _logger;

    public AiCatalogController(
        IAiCatalogService catalogService,
        ILogger<AiCatalogController> logger)
    {
        _catalogService = catalogService;
        _logger = logger;
    }

    /// <summary>
    /// Get AI catalog filtered by user's group permissions
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCatalog(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            _logger.LogWarning("[AiCatalog] Failed to resolve user from JWT");
            return Unauthorized(new { error = "Invalid authentication token or user not found." });
        }

        _logger.LogInformation("[AiCatalog] Getting filtered catalog for user: {UserId}", userId);

        var catalog = await _catalogService.GetUserCatalogAsync(userId.Value, cancellationToken);

        _logger.LogInformation("[AiCatalog] Returning {Count} accessible providers for user {UserId}",
            catalog.Count, userId);

        return Ok(new { providers = catalog });
    }
}
