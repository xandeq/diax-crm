using Asp.Versioning;
using Diax.Application.Household;
using Diax.Application.Household.Dtos;
using Diax.Shared.Results;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public class ChecklistsController : BaseApiController
{
    private readonly IChecklistCategoryService _categoryService;
    private readonly IChecklistItemService _itemService;
    private readonly ILogger<ChecklistsController> _logger;

    public ChecklistsController(
        IChecklistCategoryService categoryService,
        IChecklistItemService itemService,
        ILogger<ChecklistsController> logger)
    {
        _categoryService = categoryService;
        _itemService = itemService;
        _logger = logger;
    }

    // ===== CATEGORIES =====

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var result = await _categoryService.GetAllAsync();
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory(CreateChecklistCategoryRequest request)
    {
        var result = await _categoryService.CreateAsync(request);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPut("categories/{id}")]
    public async Task<IActionResult> UpdateCategory(Guid id, UpdateChecklistCategoryRequest request)
    {
        var result = await _categoryService.UpdateAsync(id, request);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpDelete("categories/{id}")]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        var result = await _categoryService.DeleteAsync(id);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    // ===== ITEMS =====

    [HttpGet("items")]
    public async Task<IActionResult> GetItems([FromQuery] ChecklistItemsQuery query)
    {
        var response = await _itemService.GetPagedAsync(query);
        return Ok(response);
    }

    [HttpPost("items")]
    public async Task<IActionResult> CreateItem(CreateChecklistItemRequest request)
    {
        var result = await _itemService.CreateAsync(request);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPut("items/{id}")]
    public async Task<IActionResult> UpdateItem(Guid id, UpdateChecklistItemRequest request)
    {
        var result = await _itemService.UpdateAsync(id, request);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpDelete("items/{id}")]
    public async Task<IActionResult> DeleteItem(Guid id)
    {
        var result = await _itemService.DeleteAsync(id);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    // ===== ACTIONS =====

    [HttpPatch("items/{id}/mark-bought")]
    public async Task<IActionResult> MarkBought(Guid id, [FromBody] MarkBoughtRequest? request)
    {
        var result = await _itemService.MarkBoughtAsync(id, request?.ActualPrice);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    [HttpPatch("items/{id}/mark-canceled")]
    public async Task<IActionResult> MarkCanceled(Guid id)
    {
        var result = await _itemService.MarkCanceledAsync(id);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    [HttpPatch("items/{id}/reactivate")]
    public async Task<IActionResult> Reactivate(Guid id)
    {
        var result = await _itemService.ReactivateAsync(id);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    [HttpPatch("items/{id}/archive")]
    public async Task<IActionResult> Archive(Guid id)
    {
        var result = await _itemService.ArchiveAsync(id);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    [HttpPatch("items/{id}/unarchive")]
    public async Task<IActionResult> Unarchive(Guid id)
    {
        var result = await _itemService.UnarchiveAsync(id);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    [HttpPost("items/bulk")]
    public async Task<IActionResult> BulkAction(ChecklistItemBulkRequest request)
    {
        var result = await _itemService.ExecuteBulkActionAsync(request);
        return result.IsSuccess ? Ok(new { affectedCount = result.Value }) : BadRequest(result.Error);
    }

    [HttpPost("import")]
    public async Task<IActionResult> Import(ImportChecklistRequest request)
    {
        var result = await _itemService.ImportAsync(request);
        return result.IsSuccess ? Ok(new { importedCount = result.Value }) : BadRequest(result.Error);
    }
}

public record MarkBoughtRequest(decimal? ActualPrice);
