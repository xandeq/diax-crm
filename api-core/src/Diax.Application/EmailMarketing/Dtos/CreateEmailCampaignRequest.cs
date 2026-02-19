namespace Diax.Application.EmailMarketing.Dtos;

public class CreateEmailCampaignRequest
{
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public Guid? SourceSnippetId { get; set; }
}
