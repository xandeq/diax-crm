using Diax.Domain.Common;

namespace Diax.Domain.Outreach;

/// <summary>
/// Configuração do sistema de outreach automatizado.
/// Uma por usuário.
/// </summary>
public class OutreachConfig : AuditableEntity, IUserOwnedEntity
{
    public Guid UserId { get; private set; }

    // ===== IMPORTAÇÃO =====

    /// <summary>
    /// URL do dataset Apify para importação de leads.
    /// Ex: https://api.apify.com/v2/datasets/{id}/items
    /// </summary>
    public string? ApifyDatasetUrl { get; private set; }

    /// <summary>
    /// Token de API do Apify (armazenado de forma segura).
    /// </summary>
    public string? ApifyApiToken { get; private set; }

    /// <summary>
    /// Habilita importação automática.
    /// </summary>
    public bool ImportEnabled { get; private set; }

    // ===== SEGMENTAÇÃO =====

    /// <summary>
    /// Habilita segmentação automática de leads novos.
    /// </summary>
    public bool SegmentationEnabled { get; private set; }

    // ===== ENVIO =====

    /// <summary>
    /// Habilita envio automático de emails.
    /// </summary>
    public bool SendEnabled { get; private set; }

    /// <summary>
    /// Limite diário de emails enviados.
    /// </summary>
    public int DailyEmailLimit { get; private set; } = 200;

    /// <summary>
    /// Dias mínimos entre emails para o mesmo lead.
    /// </summary>
    public int EmailCooldownDays { get; private set; } = 7;

    // ===== TEMPLATES =====

    /// <summary>
    /// Subject do template para leads HOT (JSON: { subject, body }).
    /// </summary>
    public string? HotTemplateSubject { get; private set; }
    public string? HotTemplateBody { get; private set; }

    /// <summary>
    /// Subject do template para leads WARM.
    /// </summary>
    public string? WarmTemplateSubject { get; private set; }
    public string? WarmTemplateBody { get; private set; }

    /// <summary>
    /// Subject do template para leads COLD.
    /// </summary>
    public string? ColdTemplateSubject { get; private set; }
    public string? ColdTemplateBody { get; private set; }

    // ===== WHATSAPP =====

    /// <summary>
    /// Habilita envio automático de mensagens WhatsApp.
    /// </summary>
    public bool WhatsAppSendEnabled { get; private set; }

    /// <summary>
    /// Limite diário de mensagens WhatsApp.
    /// </summary>
    public int DailyWhatsAppLimit { get; private set; } = 100;

    /// <summary>
    /// Dias mínimos entre mensagens WhatsApp para o mesmo lead.
    /// </summary>
    public int WhatsAppCooldownDays { get; private set; } = 3;

    /// <summary>
    /// Template de mensagem WhatsApp para leads HOT.
    /// Suporta placeholders: {{nome}}, {{empresa}}
    /// </summary>
    public string? WhatsAppHotTemplate { get; private set; }

    /// <summary>
    /// Template de mensagem WhatsApp para leads WARM.
    /// </summary>
    public string? WhatsAppWarmTemplate { get; private set; }

    /// <summary>
    /// Template de mensagem WhatsApp para leads COLD.
    /// </summary>
    public string? WhatsAppColdTemplate { get; private set; }

    /// <summary>
    /// Template de mensagem WhatsApp para follow-up (leads que não abriram email).
    /// </summary>
    public string? WhatsAppFollowUpTemplate { get; private set; }

    // ===== CONSTRUTORES =====

    protected OutreachConfig() { }

    public OutreachConfig(Guid userId)
    {
        UserId = userId;
    }

    // ===== MÉTODOS =====

    public void UpdateApifyConfig(string? datasetUrl, string? apiToken)
    {
        ApifyDatasetUrl = datasetUrl;
        ApifyApiToken = apiToken;
    }

    public void UpdateModuleFlags(bool importEnabled, bool segmentationEnabled, bool sendEnabled)
    {
        ImportEnabled = importEnabled;
        SegmentationEnabled = segmentationEnabled;
        SendEnabled = sendEnabled;
    }

    public void UpdateSendLimits(int dailyLimit, int cooldownDays)
    {
        DailyEmailLimit = dailyLimit > 0 ? dailyLimit : 200;
        EmailCooldownDays = cooldownDays > 0 ? cooldownDays : 7;
    }

    public void UpdateHotTemplate(string subject, string body)
    {
        HotTemplateSubject = subject;
        HotTemplateBody = body;
    }

    public void UpdateWarmTemplate(string subject, string body)
    {
        WarmTemplateSubject = subject;
        WarmTemplateBody = body;
    }

    public void UpdateColdTemplate(string subject, string body)
    {
        ColdTemplateSubject = subject;
        ColdTemplateBody = body;
    }

    public void UpdateWhatsAppFlags(bool sendEnabled)
    {
        WhatsAppSendEnabled = sendEnabled;
    }

    public void UpdateWhatsAppLimits(int dailyLimit, int cooldownDays)
    {
        DailyWhatsAppLimit = dailyLimit > 0 ? dailyLimit : 100;
        WhatsAppCooldownDays = cooldownDays > 0 ? cooldownDays : 3;
    }

    public void UpdateWhatsAppTemplates(string? hot, string? warm, string? cold, string? followUp)
    {
        WhatsAppHotTemplate = hot;
        WhatsAppWarmTemplate = warm;
        WhatsAppColdTemplate = cold;
        WhatsAppFollowUpTemplate = followUp;
    }
}
