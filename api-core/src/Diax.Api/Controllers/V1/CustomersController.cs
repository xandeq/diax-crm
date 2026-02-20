using Asp.Versioning;
using Diax.Application.Common;
using Diax.Application.Customers;
using Diax.Application.Customers.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

/// <summary>
/// Controller para gerenciamento de Customers/Leads.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public class CustomersController : BaseApiController
{
    private readonly CustomerService _customerService;
    private readonly CustomerImportService _importService;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(
        CustomerService customerService,
        CustomerImportService importService,
        ILogger<CustomersController> logger)
    {
        _customerService = customerService;
        _importService = importService;
        _logger = logger;
    }

    /// <summary>
    /// Lista customers/leads com paginação e filtros.
    /// </summary>
    /// <param name="request">Parâmetros de filtro e paginação</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Lista paginada de customers</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<CustomerResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] CustomerListRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Listando customers - Page: {Page}, PageSize: {PageSize}", request.Page, request.PageSize);

        var result = await _customerService.GetPagedAsync(request, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Obtém um customer/lead por ID.
    /// </summary>
    /// <param name="id">ID do customer</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Dados do customer</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Buscando customer por ID: {Id}", id);

        var result = await _customerService.GetByIdAsync(id, cancellationToken);

        return HandleResult(result);
    }

    /// <summary>
    /// Cria um novo customer/lead.
    /// </summary>
    /// <param name="request">Dados do novo customer</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Customer criado</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Criando novo customer: {Name} - {Email}", request.Name, request.Email);

        var result = await _customerService.CreateAsync(request, cancellationToken);

        if (result.IsFailure)
            return HandleResult(result);

        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
    }

    /// <summary>
    /// Atualiza um customer/lead existente.
    /// </summary>
    /// <param name="id">ID do customer</param>
    /// <param name="request">Dados atualizados</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Customer atualizado</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Atualizando customer: {Id}", id);

        var result = await _customerService.UpdateAsync(id, request, cancellationToken);

        return HandleResult(result);
    }

    /// <summary>
    /// Atualiza o status de um customer/lead.
    /// </summary>
    /// <param name="id">ID do customer</param>
    /// <param name="request">Novo status</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Customer atualizado</returns>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateCustomerStatusRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Atualizando status do customer {Id} para {Status}", id, request.Status);

        var result = await _customerService.UpdateStatusAsync(id, request, cancellationToken);

        return HandleResult(result);
    }

    /// <summary>
    /// Registra um contato/interação com o customer.
    /// </summary>
    /// <param name="id">ID do customer</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Customer atualizado</returns>
    [HttpPost("{id:guid}/contact")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RegisterContact(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Registrando contato com customer: {Id}", id);

        var result = await _customerService.RegisterContactAsync(id, cancellationToken);

        return HandleResult(result);
    }

    /// <summary>
    /// Converte um lead para cliente.
    /// </summary>
    /// <param name="id">ID do lead</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Customer convertido</returns>
    [HttpPost("{id:guid}/convert")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ConvertToCustomer(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Convertendo lead para customer: {Id}", id);

        var result = await _customerService.ConvertToCustomerAsync(id, cancellationToken);

        return HandleResult(result);
    }

    /// <summary>
    /// Exclui um customer/lead.
    /// </summary>
    /// <param name="id">ID do customer</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>NoContent se sucesso</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Excluindo customer: {Id}", id);

        var result = await _customerService.DeleteAsync(id, cancellationToken);

        return HandleResult(result);
    }

    /// <summary>
    /// Importa customers/leads em lote.
    /// </summary>
    /// <param name="request">Request com lista de customers a importar</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resultado da importação com contadores e erros</returns>
    [HttpPost("import")]
    [ProducesResponseType(typeof(BulkImportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkImport(
        [FromBody] BulkImportRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Importando {Count} customers", request.Customers.Count);

        var result = await _importService.ImportAsync(request, "manual-import.json", cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Retorna a timeline de atividades de um customer/lead.
    /// </summary>
    /// <param name="id">ID do customer</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Lista de eventos da timeline</returns>
    [HttpGet("{id:guid}/activities")]
    [ProducesResponseType(typeof(IEnumerable<LeadActivityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetActivities(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Buscando atividades do customer: {Id}", id);

        var result = await _customerService.GetActivitiesAsync(id, cancellationToken);

        return HandleResult(result);
    }

    /// <summary>
    /// Obtém o histórico de importações.
    /// </summary>
    /// <param name="page">Número da página (padrão: 1)</param>
    /// <param name="pageSize">Tamanho da página (padrão: 20)</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Lista paginada de importações</returns>
    [HttpGet("imports")]
    [ProducesResponseType(typeof(PagedResponse<ImportHistoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetImportHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Buscando histórico de importações - Page: {Page}, PageSize: {PageSize}", page, pageSize);

        var result = await _importService.GetImportHistoryAsync(page, pageSize, cancellationToken);

        return Ok(result);
    }
}
