using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Diax.Application.Snippets;
using Diax.Application.Snippets.Dtos;
using Diax.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Diax.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
[Authorize]
public class SnippetsController : BaseApiController
{
    private readonly ISnippetService _service;
    private readonly DiaxDbContext _db;
    private readonly ILogger<SnippetsController> _logger;

    public SnippetsController(ISnippetService service, DiaxDbContext db, ILogger<SnippetsController> logger)
    {
        _service = service;
        _db = db;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateSnippetRequestDto dto,
        CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(cancellationToken);
        if (!userId.HasValue)
            return Unauthorized();

        try
        {
            var id = await _service.CreateAsync(dto, userId.Value, cancellationToken);
            return Ok(new { id });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Snippet validation failed");
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(cancellationToken);
        if (!userId.HasValue)
            return Unauthorized();

        var result = await _service.GetAllAsync(userId.Value, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(cancellationToken);
        if (!userId.HasValue)
            return Unauthorized();

        var result = await _service.GetByIdAsync(id, userId.Value, cancellationToken);

        if (result is null)
            return NotFound();

        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(cancellationToken);
        if (!userId.HasValue)
            return Unauthorized();

        try
        {
            await _service.DeleteAsync(id, userId.Value, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [AllowAnonymous]
    [HttpGet("public/{id}")]
    public async Task<IActionResult> GetPublicById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, null, cancellationToken);

        if (result is null)
            return NotFound();

        return Ok(result);
    }

    private async Task<Guid?> ResolveUserIdAsync(CancellationToken cancellationToken)
    {
        var email = User.FindFirstValue(ClaimTypes.Email)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Email)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrWhiteSpace(email))
            return null;

        var user = await _db.AdminUsers
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Email == email, cancellationToken);

        return user?.Id;
    }
}
