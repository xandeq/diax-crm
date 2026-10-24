using Asp.Versioning;
using Diax.Application.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/service-packages")]
[Produces("application/json")]
public class ServicePackagesController : BaseApiController
{
    private readonly ServicePackageService _service;
    public ServicePackagesController(ServicePackageService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool onlyActive = false, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { Message = "Usuário não autenticado." });
        return Ok(await _service.ListAsync(userId.Value, onlyActive, ct));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertServicePackageRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { Message = "Usuário não autenticado." });
        return HandleResult(await _service.CreateAsync(request, userId.Value, ct));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertServicePackageRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { Message = "Usuário não autenticado." });
        return HandleResult(await _service.UpdateAsync(id, request, userId.Value, ct));
    }
}
