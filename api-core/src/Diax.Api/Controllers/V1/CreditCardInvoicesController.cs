using Asp.Versioning;
using Diax.Application.Finance;
using Diax.Application.Finance.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public class CreditCardInvoicesController : BaseApiController
{
    private readonly CreditCardInvoiceService _service;
    private readonly ILogger<CreditCardInvoicesController> _logger;

    public CreditCardInvoicesController(CreditCardInvoiceService service, ILogger<CreditCardInvoicesController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _service.GetAllAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("unpaid")]
    public async Task<IActionResult> GetUnpaid(CancellationToken cancellationToken)
    {
        var result = await _service.GetUnpaidInvoicesAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("creditcard/{creditCardId}")]
    public async Task<IActionResult> GetByCreditCard(Guid creditCardId, CancellationToken cancellationToken)
    {
        var result = await _service.GetByCreditCardIdAsync(creditCardId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCreditCardInvoiceRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateOrGetInvoiceAsync(request, cancellationToken);
        return result.IsSuccess ? CreatedAtAction(nameof(GetById), new { id = result.Value, version = "1" }, result.Value) : BadRequest(result.Error);
    }

    [HttpPost("{id}/pay")]
    public async Task<IActionResult> Pay(Guid id, [FromBody] PayCreditCardInvoiceRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.PayInvoiceAsync(id, request, cancellationToken);
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }

    [HttpPost("{id}/unpay")]
    public async Task<IActionResult> Unpay(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.UnpayInvoiceAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.DeleteAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }
}
