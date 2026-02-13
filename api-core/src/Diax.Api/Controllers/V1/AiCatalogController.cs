using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Diax.Application.AI;
using Diax.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Diax.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ai/catalog")]
[Authorize]
public class AiCatalogController : ControllerBase
{
    private readonly IAiCatalogService _catalogService;
    private readonly DiaxDbContext _db;
    private readonly ILogger<AiCatalogController> _logger;

    public AiCatalogController(
        IAiCatalogService catalogService,
        DiaxDbContext db,
        ILogger<AiCatalogController> logger)
    {
        _catalogService = catalogService;
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Get AI catalog filtered by user's group permissions
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCatalog(CancellationToken cancellationToken)
    {
        // 1. Resolve userId from JWT
        var userId = await ResolveUserIdAsync(cancellationToken);
        if (userId == null)
        {
            _logger.LogWarning("[AiCatalog] Failed to resolve user from JWT");
            return Unauthorized(new { error = "Invalid authentication token or user not found." });
        }

        _logger.LogInformation("[AiCatalog] Getting filtered catalog for user: {UserId}", userId);

        // 2. Get user-specific catalog (filtered by group permissions)
        var catalog = await _catalogService.GetUserCatalogAsync(userId.Value, cancellationToken);

        _logger.LogInformation("[AiCatalog] Returning {Count} accessible providers for user {UserId}",
            catalog.Count, userId);

        return Ok(new { providers = catalog });
    }

    private async Task<Guid?> ResolveUserIdAsync(CancellationToken cancellationToken)
    {
        var email = User.FindFirstValue(ClaimTypes.Email)
                    ?? User.FindFirstValue(JwtRegisteredClaimNames.Email);

        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning("[AiCatalog] No email claim found in token");
            return null;
        }

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email == email, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("[AiCatalog] User not found with email: {Email}", email);
            return null;
        }

        return user.Id;
    }
}
