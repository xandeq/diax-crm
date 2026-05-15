namespace Diax.Application.EmailMarketing.Pro.Dtos;

public class SmartPreselectResponse
{
    public List<PreselectedLeadDto> Leads { get; set; } = [];
    public int TotalSelected { get; set; }
    public Dictionary<string, int> ProviderCounts { get; set; } = [];
    public List<string> Warnings { get; set; } = [];

    // Backwards compat
    public int BrevoCount       => ProviderCounts.GetValueOrDefault("Brevo");
    public int MailjetCount     => ProviderCounts.GetValueOrDefault("Mailjet");
    public int ResendCount      => ProviderCounts.GetValueOrDefault("Resend");
    public int SendGridCount    => ProviderCounts.GetValueOrDefault("SendGrid");
    public int MailerSendCount  => ProviderCounts.GetValueOrDefault("MailerSend");
    public int ElasticEmailCount => ProviderCounts.GetValueOrDefault("ElasticEmail");
}

public class PreselectedLeadDto
{
    public Guid CustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AssignedProvider { get; set; } = string.Empty;
    public int Segment { get; set; }
    public string SegmentLabel { get; set; } = string.Empty;
    public int Score { get; set; }
    public string ReasonForSelection { get; set; } = string.Empty;
    public DateTime? LastEmailSentAt { get; set; }
}
