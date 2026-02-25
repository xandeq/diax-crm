namespace Diax.Application.Outreach.Dtos;

/// <summary>
/// DTO de requisição para atualizar configuração do outreach.
/// </summary>
public class UpdateOutreachConfigRequest
{
    public string? ApifyDatasetUrl { get; set; }
    public string? ApifyApiToken { get; set; }
    public bool ImportEnabled { get; set; }
    public bool SegmentationEnabled { get; set; }
    public bool SendEnabled { get; set; }
    public int DailyEmailLimit { get; set; } = 200;
    public int EmailCooldownDays { get; set; } = 7;
    public string? HotTemplateSubject { get; set; }
    public string? HotTemplateBody { get; set; }
    public string? WarmTemplateSubject { get; set; }
    public string? WarmTemplateBody { get; set; }
    public string? ColdTemplateSubject { get; set; }
    public string? ColdTemplateBody { get; set; }
}
