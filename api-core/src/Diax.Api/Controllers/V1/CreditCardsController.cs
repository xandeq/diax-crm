using Asp.Versioning;
using Diax.Application.Finance;
using Diax.Application.Finance.Dtos;
using Diax.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Route("api/v{version:apiVersion}/credit-cards")]
[Produces("application/json")]
public class CreditCardsController : BaseApiController
{
    private readonly CreditCardService _service;
    private readonly DiaxDbContext _db;
    private readonly ILogger<CreditCardsController> _logger;

    public CreditCardsController(CreditCardService service, DiaxDbContext db, ILogger<CreditCardsController> logger)
    {
        _service = service;
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.GetAllAsync(userId.Value, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.GetByIdAsync(id, userId.Value, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCreditCardRequest request, CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.CreateAsync(request, userId.Value, cancellationToken);
        return result.IsSuccess ? CreatedAtAction(nameof(GetById), new { id = result.Value, version = "1" }, result.Value) : BadRequest(result.Error);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCreditCardRequest request, CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.UpdateAsync(id, request, userId.Value, cancellationToken);
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.DeleteAsync(id, userId.Value, cancellationToken);
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }
}
