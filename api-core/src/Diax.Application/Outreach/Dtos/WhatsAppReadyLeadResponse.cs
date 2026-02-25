using Diax.Domain.Customers;
using Diax.Domain.Customers.Enums;

namespace Diax.Application.Outreach.Dtos;

/// <summary>
/// Lead pronto para receber WhatsApp.
/// </summary>
public class WhatsAppReadyLeadResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? WhatsApp { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string SegmentLabel { get; set; } = "";
    public int? LeadScore { get; set; }
    public int WhatsAppSentCount { get; set; }
    public DateTime? LastWhatsAppSentAt { get; set; }

    public static WhatsAppReadyLeadResponse FromEntity(Customer customer)
    {
        return new WhatsAppReadyLeadResponse
        {
            Id = customer.Id,
            Name = customer.Name,
            WhatsApp = customer.WhatsApp,
            Phone = customer.Phone,
            Email = customer.Email,
            SegmentLabel = customer.Segment?.ToString() ?? "N/A",
            LeadScore = customer.LeadScore,
            WhatsAppSentCount = customer.WhatsAppSentCount,
            LastWhatsAppSentAt = customer.LastWhatsAppSentAt
        };
    }
}
