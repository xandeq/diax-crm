namespace Diax.Application.EmailMarketing.Dtos;

public class PreviewCampaignResponse
{
    public Guid CampaignId { get; set; }
    public string TemplateSource { get; set; } = string.Empty;
    public string SubjectTemplate { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
    public string RenderedSubject { get; set; } = string.Empty;
    public string RenderedBodyHtml { get; set; } = string.Empty;
    public Dictionary<string, string?> Variables { get; set; } = [];
}
