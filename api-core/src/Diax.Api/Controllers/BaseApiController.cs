using Diax.Infrastructure.Data;
using Diax.Shared.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace Diax.Api.Controllers;

/// <summary>
/// Controller base com métodos utilitários para todos os controllers.
/// </summary>
[ApiController]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Resolve o ID do usuário logado a partir do token e do banco de dados.
    /// </summary>
    protected async Task<Guid?> ResolveUserIdAsync(DiaxDbContext db, CancellationToken ct = default)
    {
        var email = User.FindFirstValue(ClaimTypes.Email)
                 ?? User.FindFirstValue(JwtRegisteredClaimNames.Email)
                 ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrWhiteSpace(email)) return null;

        var user = await db.AdminUsers
            .AsNoTracking()
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.Email == email, ct);

        return user?.Id;
    }

    /// <summary>
    /// Converte um Result em IActionResult apropriado.
    /// </summary>
    protected IActionResult HandleResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        return HandleError(result.Error);
    }

    /// <summary>
    /// Converte um Result sem valor em IActionResult apropriado.
    /// </summary>
    protected IActionResult HandleResult(Result result)
    {
        if (result.IsSuccess)
            return NoContent();

        return HandleError(result.Error);
    }

    /// <summary>
    /// Converte um Error em IActionResult apropriado.
    /// </summary>
    private IActionResult HandleError(Error error)
    {
        return error.Code switch
        {
            var code when code.Contains("NotFound") => NotFound(new { error.Code, error.Message }),
            var code when code.Contains("Validation") => BadRequest(new { error.Code, error.Message }),
            var code when code.Contains("Conflict") => Conflict(new { error.Code, error.Message }),
            var code when code.Contains("Unauthorized") => Unauthorized(new { error.Code, error.Message }),
            var code when code.Contains("Forbidden") => Forbid(),
            _ => BadRequest(new { error.Code, error.Message })
        };
    }
}
