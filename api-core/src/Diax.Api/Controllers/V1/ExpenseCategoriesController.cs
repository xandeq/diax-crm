using Asp.Versioning;
using Diax.Application.Finance;
using Diax.Application.Finance.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/expense-categories")]
[Produces("application/json")]
public class ExpenseCategoriesController : BaseApiController
{
    private readonly ExpenseCategoryService _service;
    private readonly ILogger<ExpenseCategoriesController> _logger;

    public ExpenseCategoriesController(ExpenseCategoryService service, ILogger<ExpenseCategoriesController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
    {
        var result = await _service.GetActiveAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _service.GetAllAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateExpenseCategoryRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateExpenseCategoryRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.UpdateAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.DeactivateAsync(id, cancellationToken);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }
}
