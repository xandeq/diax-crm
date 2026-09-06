namespace Diax.Domain.Customers.Enums;

/// <summary>
/// Classificação do campo Website de um lead: site próprio do negócio vs página de
/// diretório de terceiro (econodata, redes sociais, agregadores). Porta de
/// `classify_url` em docs/email-marketing/site_check.py.
/// </summary>
public enum WebsiteKind
{
    /// <summary>Sem website, valor placeholder ('-', 'n/a', 'null', 'none') ou URL malformada.</summary>
    Unknown = 0,

    /// <summary>Domínio próprio do negócio — sinal positivo de maturidade digital.</summary>
    OwnSite = 1,

    /// <summary>Página em diretório/agregador/rede social de terceiro — não é site próprio.</summary>
    Directory = 2
}
