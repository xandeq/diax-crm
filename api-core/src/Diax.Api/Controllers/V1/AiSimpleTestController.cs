using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ai")]
public class AiSimpleTestController : ControllerBase
{
    [HttpPost("simple-test")]
    public IActionResult SimpleTest()
    {
        return Ok(new { Message = "Simple test working" });
    }
}
