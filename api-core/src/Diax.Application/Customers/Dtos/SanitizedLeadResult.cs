using Diax.Domain.Customers.Enums;

namespace Diax.Application.Customers.Dtos;

/// <summary>
/// Resultado do processo de sanitização e classificação de um Lead.
/// </summary>
public class SanitizedLeadResult
{
    // Dados Limpos
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }
    public string? CompanyName { get; set; }
    public string? Notes { get; set; }

    // Flags e Validações
    public bool IsEmailValid { get; set; }
    public bool HasSuspiciousDomain { get; set; }
    public EmailType EmailType { get; set; }

    // Classificação
    public LeadQuality Quality { get; set; }
    public bool IsEligibleForCampaigns { get; set; }

    // Status do Processamento
    public bool ShouldReject { get; set; }
    public string? RejectionReason { get; set; }
}
