using Diax.Domain.Customers;
using Diax.Domain.Customers.Enums;

namespace Diax.Application.Outreach.Dtos;

/// <summary>
/// DTO de resposta para leads prontos para envio de outreach.
/// </summary>
public class ReadyLeadResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? CompanyName { get; set; }
    public string? City { get; set; }
    public LeadSegment? Segment { get; set; }
    public int? LeadScore { get; set; }
    public DateTime? LastEmailSentAt { get; set; }
    public int EmailSentCount { get; set; }

    /// <summary>
    /// Mapeia a entidade Customer para o DTO de lead pronto.
    /// City é extraída das Tags (primeira tag que corresponde a uma cidade conhecida).
    /// </summary>
    public static ReadyLeadResponse FromEntity(Customer customer)
    {
        return new ReadyLeadResponse
        {
            Id = customer.Id,
            Name = customer.Name,
            Email = customer.Email,
            CompanyName = customer.CompanyName,
            City = ExtractCityFromTags(customer.Tags),
            Segment = customer.Segment,
            LeadScore = customer.LeadScore,
            LastEmailSentAt = customer.LastEmailSentAt,
            EmailSentCount = customer.EmailSentCount
        };
    }

    /// <summary>
    /// Tenta extrair uma cidade das tags do cliente.
    /// Procura por tags que correspondam a cidades brasileiras conhecidas.
    /// </summary>
    private static string? ExtractCityFromTags(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags))
            return null;

        var tagList = tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim().ToLowerInvariant())
            .ToList();

        var priorityCities = new[]
        {
            "São Paulo", "Rio de Janeiro", "Belo Horizonte", "Curitiba",
            "Porto Alegre", "Brasília", "Salvador", "Fortaleza",
            "Recife", "Campinas", "Florianópolis", "Goiânia",
            "Manaus", "Belém", "Vitória", "Santos",
            "Joinville", "Ribeirão Preto", "Uberlândia", "Maringá"
        };

        foreach (var city in priorityCities)
        {
            if (tagList.Any(tag => tag.Contains(city.ToLowerInvariant())))
                return city;
        }

        return null;
    }
}
