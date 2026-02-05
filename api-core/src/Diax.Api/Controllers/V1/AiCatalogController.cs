using Asp.Versioning;
using Diax.Application.AI;
using Diax.Infrastructure.Data;
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
    private readonly DiaxDbContext _db;

    public AiCatalogController(IAiCatalogService catalogService, DiaxDbContext db)
    {
        _catalogService = catalogService;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetCatalog(CancellationToken cancellationToken)
    {
        // Usar o mesmo padrão de resolução de UserId dos outros controllers
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue)
        {
            return Unauthorized(new { error = "User not found or not authenticated." });
        }

        var catalog = await _catalogService.GetUserCatalogAsync(userId.Value, cancellationToken);
        return Ok(new { providers = catalog });
    }
}
