using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

/// <summary>
/// Controller de teste isolado para diagnóstico.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public class TestController : ControllerBase
{
    /// <summary>
    /// Endpoint de teste sem nenhuma dependência.
    /// </summary>
    [HttpGet("isolated")]
    public IActionResult Isolated()
    {
        return Ok(new {
            message = "Isolated endpoint working",
            timestamp = DateTime.UtcNow
        });
    }
}
