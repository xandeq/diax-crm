namespace Diax.Application.EmailMarketing;

/// <summary>
/// URLs públicas usadas dentro dos emails (unsubscribe, CTA default).
/// PublicBaseUrl DEVE apontar para o host real da API — links quebrados de
/// unsubscribe são violação de compliance (LGPD/CAN-SPAM).
/// </summary>
public class EmailLinkOptions
{
    public const string Section = "EmailLinks";

    /// <summary>Base pública da API que serve GET /unsubscribe (sem barra final).</summary>
    public string PublicBaseUrl { get; set; } = "https://api.alexandrequeiroz.com.br";

    /// <summary>CTA default quando a campanha usa {{cta_link}} sem definir o próprio.</summary>
    public string DefaultCtaUrl { get; set; } = "https://www.alexandrequeiroz.com.br";

    /// <summary>Chave HMAC do token de unsubscribe — preenchida a partir de Jwt:Key no DI.</summary>
    public string SigningKey { get; set; } = string.Empty;
}
