using Diax.Application.Common;
using Diax.Domain.Customers.Enums;

namespace Diax.Application.Customers.Dtos;

/// <summary>
/// DTO para listagem paginada de Customers com filtros.
/// </summary>
public class CustomerListRequest : PagedRequest
{
    /// <summary>
    /// Busca por nome, e-mail ou empresa.
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Filtro por status.
    /// </summary>
    public CustomerStatus? Status { get; set; }

    /// <summary>
    /// Filtro por origem.
    /// </summary>
    public LeadSource? Source { get; set; }

    /// <summary>
    /// Se true, retorna apenas leads.
    /// </summary>
    public bool? OnlyLeads { get; set; }

    /// <summary>
    /// Se true, retorna apenas clientes ativos.
    /// </summary>
    public bool? OnlyCustomers { get; set; }
}
