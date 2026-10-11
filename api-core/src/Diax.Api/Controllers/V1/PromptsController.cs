using Asp.Versioning;
using Diax.Application.PromptLibrary;
using Diax.Application.PromptLibrary.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

/// <summary>Prompt Studio — biblioteca de prompts com versões, tags, coleções e variáveis.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/prompts")]
[Produces("application/json")]
[Authorize]
public class PromptsController : BaseApiController
{
    private readonly IPromptLibraryService _service;
    private readonly IPromptTransferService _transfer;

    public PromptsController(IPromptLibraryService service, IPromptTransferService transfer)
    {
        _service = service;
        _transfer = transfer;
    }

    /// <summary>
    /// Exporta no formato .md canônico. <c>format=md</c> com 1 id → arquivo .md; senão → .zip.
    /// <c>flavor=claude-command</c> troca {{var}} por $ARGUMENTS/$N (pronto para ~/.claude/commands).
    /// </summary>
    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] string format = "zip", [FromQuery] string flavor = "diax",
        [FromQuery] List<Guid>? ids = null, CancellationToken ct = default)
    {
        var result = await _transfer.ExportAsync(ids, format, flavor, ct);
        if (result.IsFailure) return HandleResult(result);
        var f = result.Value!;
        return File(f.Content, f.ContentType, f.FileName);
    }

    /// <summary>Importa .md/.txt/.zip. <c>dryRun=true</c> (padrão) só gera o relatório, sem gravar.</summary>
    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(16 * 1024 * 1024)]
    public async Task<IActionResult> Import([FromForm] IFormFileCollection files, [FromForm] bool dryRun = true,
        [FromForm] string strategy = "new-version", CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();
        if (files is null || files.Count == 0) return BadRequest(new { code = "Prompt.Validation", message = "Envie ao menos um arquivo." });
        if (files.Count > 50) return BadRequest(new { code = "Prompt.Validation", message = "Máximo de 50 arquivos por vez (use um .zip para mais)." });

        var uploads = new List<ImportUpload>();
        foreach (var file in files)
        {
            if (file.Length > PromptTransferService.MaxZipBytes)
                return StatusCode(StatusCodes.Status413PayloadTooLarge, new { code = "Prompt.Validation", message = $"'{file.FileName}' passa de 5 MB." });
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms, ct);
            uploads.Add(new ImportUpload(Path.GetFileName(file.FileName), ms.ToArray()));
        }

        return HandleResult(await _transfer.ImportAsync(uploads, dryRun, strategy, userId.Value, ct));
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PromptListQuery query, CancellationToken ct)
        => Ok(await _service.ListAsync(query, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        => HandleResult(await _service.GetAsync(id, ct));

    [HttpGet("by-slug/{slug}")]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct)
        => HandleResult(await _service.GetBySlugAsync(slug, ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePromptRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();
        var result = await _service.CreateAsync(request, userId.Value, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value!.Prompt.Id, version = "1.0" }, result.Value)
            : HandleResult(result);
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePromptRequest request, CancellationToken ct)
        => HandleResult(await _service.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => HandleResult(await _service.DeleteAsync(id, ct));

    [HttpPost("{id:guid}/duplicate")]
    public async Task<IActionResult> Duplicate(Guid id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();
        return HandleResult(await _service.DuplicateAsync(id, userId.Value, ct));
    }

    [HttpPost("{id:guid}/use")]
    public async Task<IActionResult> Use(Guid id, CancellationToken ct)
        => HandleResult(await _service.RegisterUseAsync(id, ct));

    [HttpPost("{id:guid}/render")]
    public async Task<IActionResult> Render(Guid id, [FromBody] RenderPromptRequest request, CancellationToken ct)
        => HandleResult(await _service.RenderAsync(id, request, ct));

    [HttpPost("by-slug/{slug}/render")]
    public async Task<IActionResult> RenderBySlug(string slug, [FromBody] RenderPromptRequest request, CancellationToken ct)
    {
        var prompt = await _service.GetBySlugAsync(slug, ct);
        if (prompt.IsFailure) return HandleResult(prompt);
        return HandleResult(await _service.RenderAsync(prompt.Value!.Prompt.Id, request, ct));
    }

    [HttpGet("{id:guid}/versions")]
    public async Task<IActionResult> Versions(Guid id, CancellationToken ct)
        => HandleResult(await _service.GetVersionsAsync(id, ct));

    [HttpGet("{id:guid}/versions/{number:int}")]
    public async Task<IActionResult> Version(Guid id, int number, CancellationToken ct)
        => HandleResult(await _service.GetVersionAsync(id, number, ct));

    [HttpPost("{id:guid}/versions")]
    public async Task<IActionResult> AddVersion(Guid id, [FromBody] AddPromptVersionRequest request, CancellationToken ct)
        => await VersionResponseAsync(id, await _service.AddVersionAsync(id, request, ct), ct);

    [HttpPost("{id:guid}/versions/{number:int}/restore")]
    public async Task<IActionResult> Restore(Guid id, int number, CancellationToken ct)
        => await VersionResponseAsync(id, await _service.RestoreVersionAsync(id, number, ct), ct);

    [HttpGet("tags")]
    public async Task<IActionResult> Tags(CancellationToken ct)
        => Ok(await _service.GetTagsAsync(ct));

    [HttpPost("tags/rename")]
    public async Task<IActionResult> RenameTag([FromBody] RenameTagRequest request, CancellationToken ct)
    {
        var result = await _service.RenameTagAsync(request, ct);
        return result.IsSuccess ? Ok(new { updated = result.Value }) : HandleResult(result);
    }

    [HttpPost("from-generator/{userPromptId:guid}")]
    public async Task<IActionResult> FromGenerator(Guid userPromptId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();
        return HandleResult(await _service.CreateFromGeneratorAsync(userPromptId, userId.Value, ct));
    }

    [HttpPost("from-snippet/{snippetId:guid}")]
    public async Task<IActionResult> FromSnippet(Guid snippetId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();
        return HandleResult(await _service.CreateFromSnippetAsync(snippetId, userId.Value, ct));
    }

    /// <summary>201 = versão criada; 200 + X-Prompt-NoChange = conteúdo idêntico; 409 traz a versão atual.</summary>
    private async Task<IActionResult> VersionResponseAsync(Guid id, Diax.Shared.Results.Result<AddPromptVersionResult> result, CancellationToken ct)
    {
        if (result.IsSuccess)
        {
            if (result.Value!.Created) return StatusCode(StatusCodes.Status201Created, result.Value.Version);
            Response.Headers["X-Prompt-NoChange"] = "true";
            return Ok(result.Value.Version);
        }

        if (result.Error.Code == PromptLibraryService.ErrConflict)
        {
            var current = await _service.GetAsync(id, ct);
            return Conflict(new { result.Error.Code, result.Error.Message, current = current.IsSuccess ? current.Value!.Current : null });
        }
        return HandleResult(result);
    }
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/prompt-collections")]
[Produces("application/json")]
[Authorize]
public class PromptCollectionsController : BaseApiController
{
    private readonly IPromptCollectionService _service;

    public PromptCollectionsController(IPromptCollectionService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await _service.ListAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SavePromptCollectionRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();
        return HandleResult(await _service.CreateAsync(request, userId.Value, ct));
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SavePromptCollectionRequest request, CancellationToken ct)
        => HandleResult(await _service.UpdateAsync(id, request, ct));

    [HttpPut("order")]
    public async Task<IActionResult> Reorder([FromBody] ReorderCollectionsRequest request, CancellationToken ct)
        => HandleResult(await _service.ReorderAsync(request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => HandleResult(await _service.DeleteAsync(id, ct));
}
