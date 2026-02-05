using System.IdentityModel.Tokens.Jwt;
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
    private readonly ILogger<AiCatalogController> _logger;

    public AiCatalogController(
        IAiCatalogService catalogService,
        ILogger<AiCatalogController> logger)
    {
        _catalogService = catalogService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetCatalog(CancellationToken cancellationToken)
    {
        var email = User.FindFirstValue(ClaimTypes.Email) 
                    ?? User.FindFirstValue(JwtRegisteredClaimNames.Email);

        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning("[AiCatalog] No email claim found in token");
            return Unauthorized(new { error = "Invalid authentication token." });
        }

        _logger.LogInformation("[AiCatalog] Getting catalog for user: {Email}", email);

        // Usar GetCatalogAsync que tem fallback para configuração
        var catalog = await _catalogService.GetCatalogAsync(cancellationToken);
        
        _logger.LogInformation("[AiCatalog] Returning {Count} providers", catalog.Count);
        
        return Ok(new { providers = catalog });
    }
}
