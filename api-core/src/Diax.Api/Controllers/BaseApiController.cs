using Diax.Shared.Results;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers;

/// <summary>
/// Controller base com métodos utilitários para todos os controllers.
/// </summary>
[ApiController]
public abstract class BaseApiController : ControllerBase
{
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
