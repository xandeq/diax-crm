using System.Security.Claims;
using Asp.Versioning;
using Diax.Application.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ai/catalog")]
[Authorize]
public class AiCatalogController : ControllerBase
{
    private readonly IAiCatalogService _catalogService;

    public AiCatalogController(IAiCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCatalog(CancellationToken cancellationToken)
    {
        // Extract User ID from Claims
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized();
        }

        var catalog = await _catalogService.GetUserCatalogAsync(userId, cancellationToken);
        return Ok(new { providers = catalog });
    }
}
