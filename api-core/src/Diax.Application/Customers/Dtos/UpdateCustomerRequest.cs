using Diax.Domain.Customers.Enums;

namespace Diax.Application.Customers.Dtos;

/// <summary>
/// DTO para atualização de um Customer/Lead existente.
/// </summary>
public class UpdateCustomerRequest
{
    // Informações básicas
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public PersonType PersonType { get; set; }
    public string? CompanyName { get; set; }
    public string? Document { get; set; }

    // Contato
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }
    public string? SecondaryEmail { get; set; }
    public string? Website { get; set; }

    // Origem
    public LeadSource Source { get; set; }
    public string? SourceDetails { get; set; }
    public string? Notes { get; set; }
    public string? Tags { get; set; }
}
