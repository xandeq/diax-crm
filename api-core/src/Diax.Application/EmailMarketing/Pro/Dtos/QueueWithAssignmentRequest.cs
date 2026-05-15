namespace Diax.Application.EmailMarketing.Pro.Dtos;

public class QueueWithAssignmentRequest
{
    public Guid CampaignId { get; set; }
    public List<AssignedLeadQueueDto> Leads { get; set; } = [];
}

public class AssignedLeadQueueDto
{
    public Guid CustomerId { get; set; }
    public string AssignedProvider { get; set; } = string.Empty; // Brevo | Mailjet | Resend | ElasticEmail | MailerSend | SendGrid
}
