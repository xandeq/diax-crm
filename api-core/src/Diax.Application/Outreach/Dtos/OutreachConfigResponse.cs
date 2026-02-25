using Diax.Domain.Outreach;

namespace Diax.Application.Outreach.Dtos;

/// <summary>
/// DTO de resposta para configuração do outreach.
/// </summary>
public class OutreachConfigResponse
{
    public Guid Id { get; set; }
    public string? ApifyDatasetUrl { get; set; }
    public string? ApifyApiToken { get; set; }
    public bool ImportEnabled { get; set; }
    public bool SegmentationEnabled { get; set; }
    public bool SendEnabled { get; set; }
    public int DailyEmailLimit { get; set; }
    public int EmailCooldownDays { get; set; }
    public string? HotTemplateSubject { get; set; }
    public string? HotTemplateBody { get; set; }
    public string? WarmTemplateSubject { get; set; }
    public string? WarmTemplateBody { get; set; }
    public string? ColdTemplateSubject { get; set; }
    public string? ColdTemplateBody { get; set; }

    // ===== WHATSAPP =====
    public bool WhatsAppSendEnabled { get; set; }
    public int DailyWhatsAppLimit { get; set; }
    public int WhatsAppCooldownDays { get; set; }
    public string? WhatsAppHotTemplate { get; set; }
    public string? WhatsAppWarmTemplate { get; set; }
    public string? WhatsAppColdTemplate { get; set; }
    public string? WhatsAppFollowUpTemplate { get; set; }

    /// <summary>
    /// Mapeia a entidade OutreachConfig para o DTO de resposta.
    /// </summary>
    public static OutreachConfigResponse FromEntity(OutreachConfig config)
    {
        return new OutreachConfigResponse
        {
            Id = config.Id,
            ApifyDatasetUrl = config.ApifyDatasetUrl,
            ApifyApiToken = config.ApifyApiToken,
            ImportEnabled = config.ImportEnabled,
            SegmentationEnabled = config.SegmentationEnabled,
            SendEnabled = config.SendEnabled,
            DailyEmailLimit = config.DailyEmailLimit,
            EmailCooldownDays = config.EmailCooldownDays,
            HotTemplateSubject = config.HotTemplateSubject,
            HotTemplateBody = config.HotTemplateBody,
            WarmTemplateSubject = config.WarmTemplateSubject,
            WarmTemplateBody = config.WarmTemplateBody,
            ColdTemplateSubject = config.ColdTemplateSubject,
            ColdTemplateBody = config.ColdTemplateBody,
            WhatsAppSendEnabled = config.WhatsAppSendEnabled,
            DailyWhatsAppLimit = config.DailyWhatsAppLimit,
            WhatsAppCooldownDays = config.WhatsAppCooldownDays,
            WhatsAppHotTemplate = config.WhatsAppHotTemplate,
            WhatsAppWarmTemplate = config.WhatsAppWarmTemplate,
            WhatsAppColdTemplate = config.WhatsAppColdTemplate,
            WhatsAppFollowUpTemplate = config.WhatsAppFollowUpTemplate
        };
    }
}
