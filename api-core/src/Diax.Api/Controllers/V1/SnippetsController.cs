using Asp.Versioning;
using Diax.Application.Snippets;
using Diax.Application.Snippets.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
[Authorize]
public class SnippetsController : BaseApiController
{
    private const long MaxFileSizeBytes = 20 * 1024 * 1024; // 20 MB

    private readonly ISnippetService _service;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<SnippetsController> _logger;

    public SnippetsController(ISnippetService service, IWebHostEnvironment env, ILogger<SnippetsController> logger)
    {
        _service = service;
        _env = env;
        _logger = logger;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(110_100_480)]
    public async Task<IActionResult> Create(
        [FromForm] CreateSnippetRequestDto dto,
        [FromForm] IFormFileCollection? files,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var savedPaths = new List<string>();

        try
        {
            var attachmentDtos = new List<SnippetAttachmentUploadDto>();

            if (files != null)
            {
                foreach (var file in files)
                {
                    if (file.Length == 0) continue;

                    if (file.Length > MaxFileSizeBytes)
                        return BadRequest(new { message = $"O arquivo '{file.FileName}' excede o limite de 20 MB." });

                    var storedName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                    var dir = GetStorageDir(userId.Value);
                    Directory.CreateDirectory(dir);
                    var fullPath = Path.Combine(dir, storedName);

                    await using (var fs = System.IO.File.Create(fullPath))
                        await file.CopyToAsync(fs, cancellationToken);

                    savedPaths.Add(fullPath);
                    attachmentDtos.Add(new SnippetAttachmentUploadDto(file.FileName, storedName, file.ContentType, file.Length));
                }
            }

            var id = await _service.CreateAsync(dto, userId.Value, attachmentDtos, cancellationToken);
            return Ok(new { id });
        }
        catch (ArgumentException ex)
        {
            CleanupFiles(savedPaths);
            _logger.LogWarning(ex, "Snippet validation failed");
            return BadRequest(new { message = ex.Message });
        }
        catch
        {
            CleanupFiles(savedPaths);
            throw;
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var result = await _service.GetAllAsync(userId.Value, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
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
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        try
        {
            var storedNames = await _service.GetAttachmentStoredNamesAsync(id, userId.Value, cancellationToken);

            await _service.DeleteAsync(id, userId.Value, cancellationToken);

            var dir = GetStorageDir(userId.Value);
            foreach (var name in storedNames)
            {
                var path = Path.Combine(dir, name);
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }

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

    [HttpPost("{id}/attachments")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(22_020_096)]
    public async Task<IActionResult> AddAttachment(
        Guid id,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Arquivo inválido ou vazio." });

        if (file.Length > MaxFileSizeBytes)
            return BadRequest(new { message = "O arquivo excede o limite de 20 MB." });

        var storedName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var dir = GetStorageDir(userId.Value);
        Directory.CreateDirectory(dir);
        var fullPath = Path.Combine(dir, storedName);

        try
        {
            await using (var fs = System.IO.File.Create(fullPath))
                await file.CopyToAsync(fs, cancellationToken);

            var attachmentId = await _service.AddAttachmentAsync(
                id,
                userId.Value,
                new SnippetAttachmentUploadDto(file.FileName, storedName, file.ContentType, file.Length),
                cancellationToken);

            return Ok(new { id = attachmentId });
        }
        catch (KeyNotFoundException)
        {
            CleanupFiles(new[] { fullPath });
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            CleanupFiles(new[] { fullPath });
            return Forbid();
        }
        catch
        {
            CleanupFiles(new[] { fullPath });
            throw;
        }
    }

    [HttpGet("{id}/attachments/{attachmentId}/download")]
    public async Task<IActionResult> DownloadAttachment(Guid id, Guid attachmentId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var info = await _service.GetAttachmentDownloadInfoAsync(id, attachmentId, userId.Value, cancellationToken);
        if (info is null)
            return NotFound();

        var filePath = Path.Combine(GetStorageDir(info.Value.OwnerUserId), info.Value.StoredFileName);
        if (!System.IO.File.Exists(filePath))
            return NotFound(new { message = "Arquivo não encontrado no servidor." });

        var stream = System.IO.File.OpenRead(filePath);
        return File(stream, info.Value.ContentType, info.Value.OriginalFileName);
    }

    [HttpDelete("{id}/attachments/{attachmentId}")]
    public async Task<IActionResult> DeleteAttachment(Guid id, Guid attachmentId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        try
        {
            var storedFileName = await _service.DeleteAttachmentAsync(id, attachmentId, userId.Value, cancellationToken);

            if (storedFileName is not null)
            {
                var path = Path.Combine(GetStorageDir(userId.Value), storedFileName);
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }

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

    [AllowAnonymous]
    [HttpGet("public/{id}/attachments/{attachmentId}/download")]
    public async Task<IActionResult> DownloadPublicAttachment(Guid id, Guid attachmentId, CancellationToken cancellationToken)
    {
        var info = await _service.GetAttachmentDownloadInfoAsync(id, attachmentId, null, cancellationToken);
        if (info is null)
            return NotFound();

        var filePath = Path.Combine(GetStorageDir(info.Value.OwnerUserId), info.Value.StoredFileName);
        if (!System.IO.File.Exists(filePath))
            return NotFound(new { message = "Arquivo não encontrado no servidor." });

        var stream = System.IO.File.OpenRead(filePath);
        return File(stream, info.Value.ContentType, info.Value.OriginalFileName);
    }

    private string GetStorageDir(Guid userId) =>
        Path.Combine(_env.ContentRootPath, "App_Data", "snippets", userId.ToString());

    private static void CleanupFiles(IEnumerable<string> paths)
    {
        foreach (var path in paths)
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
    }
}
