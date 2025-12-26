using Diax.Domain.Customers.Enums;

namespace Diax.Application.Customers.Dtos;

/// <summary>
/// DTO para criação de um novo Customer/Lead.
/// </summary>
public class CreateCustomerRequest
{
    // Obrigatórios
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // Opcionais com defaults
    public PersonType PersonType { get; set; } = PersonType.Individual;
    public LeadSource Source { get; set; } = LeadSource.Manual;

    // Opcionais
    public string? CompanyName { get; set; }
    public string? Document { get; set; }
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }
    public string? SecondaryEmail { get; set; }
    public string? Website { get; set; }
    public string? SourceDetails { get; set; }
    public string? Notes { get; set; }
    public string? Tags { get; set; }
}
