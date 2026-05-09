namespace Diax.Application.EmailMarketing.Pro.Dtos;

public class SmartPreselectResponse
{
    public List<PreselectedLeadDto> Leads { get; set; } = [];
    public int TotalSelected { get; set; }
    public int BrevoCount { get; set; }
    public int MailjetCount { get; set; }
    public int ResendCount { get; set; }
    public List<string> Warnings { get; set; } = [];
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
