namespace Diax.Application.EmailMarketing.Dtos;

public class PreviewCampaignRequest
{
    public string? SubjectOverride { get; set; }
    public string? BodyHtmlOverride { get; set; }
    public Guid? SourceSnippetIdOverride { get; set; }
    public PreviewCampaignMockDataRequest MockData { get; set; } = new();
}

public class PreviewCampaignMockDataRequest
{
    public string? FirstName { get; set; }
    public string? Email { get; set; }
    public string? Company { get; set; }
    public string? LeadStatus { get; set; }
}
