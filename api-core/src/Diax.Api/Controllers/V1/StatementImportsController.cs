using Asp.Versioning;
using Diax.Application.Finance;
using Diax.Application.Finance.Dtos;
using Diax.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class StatementImportsController : BaseApiController
{
    private readonly StatementImportService _service;
    private readonly DiaxDbContext _db;

    public StatementImportsController(StatementImportService service, DiaxDbContext db)
    {
        _service = service;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.GetAllAsync(userId.Value, ct);
        return HandleResult(result);
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(
        [FromForm] UploadStatementRequest request,
        IFormFile file,
        CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (!userId.HasValue) return Unauthorized();

        if (file == null || file.Length == 0)
        {
            return BadRequest("Arquivo inválido.");
        }

        using var stream = file.OpenReadStream();
        var result = await _service.UploadAsync(
            request,
            file.FileName,
            file.ContentType,
            file.Length,
            stream,
            userId.Value,
            ct);

        return HandleResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.GetDetailAsync(id, userId.Value, ct);
        return HandleResult(result);
    }

    [HttpGet("{id:guid}/preview-post")]
    public async Task<IActionResult> PreviewPost(Guid id, CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.PreviewPostAsync(id, userId.Value, ct);
        return HandleResult(result);
    }

    [HttpPost("{id:guid}/post")]
    public async Task<IActionResult> Post(Guid id, [FromBody] StatementImportPostRequest request, CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.PostAsync(id, request, userId.Value, ct);
        return HandleResult(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.DeleteAsync(id, userId.Value, ct);
        return HandleResult(result);
    }
}
