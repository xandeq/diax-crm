using Asp.Versioning;
using Diax.Application.TaxDocuments;
using Diax.Application.TaxDocuments.DTOs;
using Diax.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tax-documents")]
[Produces("application/json")]
public class TaxDocumentsController : BaseApiController
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/jpg",
        "image/png",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };

    private const long MaxFileSizeBytes = 20 * 1024 * 1024; // 20 MB

    private readonly ITaxDocumentService _service;
    private readonly DiaxDbContext _db;
    private readonly IWebHostEnvironment _env;

    public TaxDocumentsController(ITaxDocumentService service, DiaxDbContext db, IWebHostEnvironment env)
    {
        _service = service;
        _db = db;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? fiscalYear,
        [FromQuery] int? institutionType,
        [FromQuery] string? search,
        CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (!userId.HasValue) return Unauthorized();

        var request = new TaxDocumentListRequest(
            fiscalYear,
            institutionType.HasValue ? (Domain.TaxDocuments.TaxDocumentType)institutionType.Value : null,
            search);

        var result = await _service.GetAllAsync(userId.Value, request, ct);
        return HandleResult(result);
    }

    [HttpGet("fiscal-years")]
    public async Task<IActionResult> GetFiscalYears(CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.GetFiscalYearsAsync(userId.Value, ct);
        return HandleResult(result);
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(26_214_400)] // 25 MB
    public async Task<IActionResult> Upload(
        [FromForm] UploadTaxDocumentRequest request,
        IFormFile file,
        CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (!userId.HasValue) return Unauthorized();

        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Arquivo inválido ou vazio." });

        if (file.Length > MaxFileSizeBytes)
            return BadRequest(new { message = "O arquivo excede o limite de 20 MB." });

        if (!AllowedContentTypes.Contains(file.ContentType))
            return BadRequest(new { message = "Tipo de arquivo não permitido. Use PDF, JPG, PNG ou Word." });

        var ext = Path.GetExtension(file.FileName);
        var storedFileName = $"{Guid.NewGuid()}{ext}";
        var dir = GetStorageDir(userId.Value);
        Directory.CreateDirectory(dir);
        var filePath = Path.Combine(dir, storedFileName);

        await using (var fs = System.IO.File.Create(filePath))
            await file.CopyToAsync(fs, ct);

        var result = await _service.CreateAsync(
            userId.Value,
            request,
            file.FileName,
            storedFileName,
            file.ContentType,
            file.Length,
            ct);

        if (result.IsFailure)
        {
            // Clean up file if DB save failed
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
        }

        return HandleResult(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaxDocumentRequest request, CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.UpdateAsync(userId.Value, id, request, ct);
        return HandleResult(result);
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.GetDownloadInfoAsync(userId.Value, id, ct);
        if (result.IsFailure)
            return HandleResult(result);

        var (storedFileName, originalFileName, contentType) = result.Value;
        var filePath = Path.Combine(GetStorageDir(userId.Value), storedFileName);

        if (!System.IO.File.Exists(filePath))
            return NotFound(new { message = "Arquivo não encontrado no servidor." });

        var stream = System.IO.File.OpenRead(filePath);
        return File(stream, contentType, originalFileName);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (!userId.HasValue) return Unauthorized();

        var result = await _service.DeleteAsync(userId.Value, id, ct);
        if (result.IsFailure)
            return HandleResult(result);

        var storedFileName = result.Value;
        var filePath = Path.Combine(GetStorageDir(userId.Value), storedFileName);
        if (System.IO.File.Exists(filePath))
            System.IO.File.Delete(filePath);

        return NoContent();
    }

    private string GetStorageDir(Guid userId) =>
        Path.Combine(_env.ContentRootPath, "App_Data", "tax-documents", userId.ToString());
}
