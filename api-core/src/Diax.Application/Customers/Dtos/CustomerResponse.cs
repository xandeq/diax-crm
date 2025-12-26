using Diax.Domain.Customers;
using Diax.Domain.Customers.Enums;

namespace Diax.Application.Customers.Dtos;

/// <summary>
/// DTO de resposta para Customer.
/// </summary>
public class CustomerResponse
{
    public Guid Id { get; set; }

    // Identidade
    public string Name { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public PersonType PersonType { get; set; }
    public string PersonTypeDescription => PersonType == PersonType.Individual ? "Pessoa Física" : "Pessoa Jurídica";
    public string? Document { get; set; }

    // Contato
    public string Email { get; set; } = string.Empty;
    public string? SecondaryEmail { get; set; }
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }
    public string? Website { get; set; }

    // Origem
    public LeadSource Source { get; set; }
    public string SourceDescription { get; set; } = string.Empty;
    public string? SourceDetails { get; set; }
    public string? Notes { get; set; }
    public string? Tags { get; set; }
    public List<string> TagList => string.IsNullOrEmpty(Tags)
        ? []
        : Tags.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList();

    // Status
    public CustomerStatus Status { get; set; }
    public string StatusDescription { get; set; } = string.Empty;
    public bool IsLead { get; set; }
    public bool IsActiveCustomer { get; set; }
    public DateTime? ConvertedAt { get; set; }
    public DateTime? LastContactAt { get; set; }

    // Auditoria
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Mapeia uma entidade Customer para o DTO de resposta.
    /// </summary>
    public static CustomerResponse FromEntity(Customer customer)
    {
        return new CustomerResponse
        {
            Id = customer.Id,
            Name = customer.Name,
            CompanyName = customer.CompanyName,
            PersonType = customer.PersonType,
            Document = customer.Document,
            Email = customer.Email,
            SecondaryEmail = customer.SecondaryEmail,
            Phone = customer.Phone,
            WhatsApp = customer.WhatsApp,
            Website = customer.Website,
            Source = customer.Source,
            SourceDescription = GetSourceDescription(customer.Source),
            SourceDetails = customer.SourceDetails,
            Notes = customer.Notes,
            Tags = customer.Tags,
            Status = customer.Status,
            StatusDescription = GetStatusDescription(customer.Status),
            IsLead = customer.IsLead,
            IsActiveCustomer = customer.IsActiveCustomer,
            ConvertedAt = customer.ConvertedAt,
            LastContactAt = customer.LastContactAt,
            CreatedAt = customer.CreatedAt,
            CreatedBy = customer.CreatedBy,
            UpdatedAt = customer.UpdatedAt,
            UpdatedBy = customer.UpdatedBy
        };
    }

    private static string GetStatusDescription(CustomerStatus status) => status switch
    {
        CustomerStatus.Lead => "Lead",
        CustomerStatus.Contacted => "Contactado",
        CustomerStatus.Qualified => "Qualificado",
        CustomerStatus.Negotiating => "Em Negociação",
        CustomerStatus.Customer => "Cliente",
        CustomerStatus.Inactive => "Inativo",
        CustomerStatus.Churned => "Cancelado",
        _ => "Desconhecido"
    };

    private static string GetSourceDescription(LeadSource source) => source switch
    {
        LeadSource.Unknown => "Não especificado",
        LeadSource.Manual => "Cadastro Manual",
        LeadSource.Website => "Site",
        LeadSource.Referral => "Indicação",
        LeadSource.Scraping => "Scraping",
        LeadSource.SocialMedia => "Redes Sociais",
        LeadSource.EmailCampaign => "E-mail Marketing",
        LeadSource.PaidAds => "Anúncios Pagos",
        LeadSource.Event => "Evento",
        LeadSource.Partnership => "Parceria",
        LeadSource.Import => "Importação",
        _ => "Desconhecido"
    };
}
