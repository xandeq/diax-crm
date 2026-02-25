namespace Diax.Domain.Customers.Enums;

/// <summary>
/// Origem do lead/cliente.
/// De onde veio esse contato.
/// </summary>
public enum LeadSource
{
    /// <summary>
    /// Origem não especificada.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Cadastro manual pelo time.
    /// </summary>
    Manual = 1,

    /// <summary>
    /// Formulário do site.
    /// </summary>
    Website = 2,

    /// <summary>
    /// Indicação de outro cliente.
    /// </summary>
    Referral = 3,

    /// <summary>
    /// Scraping de e-mails (Google, etc).
    /// </summary>
    Scraping = 4,

    /// <summary>
    /// Redes sociais (Instagram, LinkedIn, etc).
    /// </summary>
    SocialMedia = 5,

    /// <summary>
    /// Campanhas de e-mail marketing.
    /// </summary>
    EmailCampaign = 6,

    /// <summary>
    /// Anúncios pagos (Google Ads, Meta Ads).
    /// </summary>
    PaidAds = 7,

    /// <summary>
    /// Evento presencial ou online.
    /// </summary>
    Event = 8,

    /// <summary>
    /// Parceria com outra empresa.
    /// </summary>
    Partnership = 9,

    /// <summary>
    /// Importação de lista externa.
    /// </summary>
    Import = 10,

    /// <summary>
    /// Scraping via Apify Google Maps.
    /// </summary>
    GoogleMaps = 11
}
