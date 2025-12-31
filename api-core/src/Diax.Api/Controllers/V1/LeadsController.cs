using Diax.Api.Controllers;
using Diax.Application.Common;
using Diax.Application.Customers;
using Diax.Application.Customers.Dtos;
using Diax.Domain.Customers.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[Authorize]
[Route("api/v1/leads")]
public class LeadsController : BaseApiController
{
    private readonly CustomerService _service;

    public LeadsController(CustomerService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<CustomerResponse>>> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] CustomerStatus? status = null)
    {
        var request = new CustomerListRequest
        {
            Page = page,
            PageSize = pageSize,
            Search = search,
            Status = status,
            OnlyLeads = true
        };

        var result = await _service.GetPagedAsync(request);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerResponse>> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return result.Match<ActionResult<CustomerResponse>>(
            onSuccess: Ok,
            onFailure: HandleFailure);
    }

    [HttpPost]
    public async Task<ActionResult<CustomerResponse>> Create([FromBody] CreateCustomerRequest request)
    {
        // Força status inicial como Lead se não especificado, mas o serviço já deve tratar isso.
        // O CreateCustomerRequest tem Source, etc.

        var result = await _service.CreateAsync(request);
        return result.Match<ActionResult<CustomerResponse>>(
            onSuccess: response => CreatedAtAction(nameof(GetById), new { id = response.Id }, response),
            onFailure: HandleFailure);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CustomerResponse>> Update(Guid id, [FromBody] UpdateCustomerRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        return result.Match<ActionResult<CustomerResponse>>(
            onSuccess: Ok,
            onFailure: HandleFailure);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);
        return result.Match<ActionResult>(
            onSuccess: () => NoContent(),
            onFailure: HandleFailure);
    }
}
