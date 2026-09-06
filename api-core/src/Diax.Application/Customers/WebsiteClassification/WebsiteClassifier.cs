using Diax.Domain.Customers.Enums;

namespace Diax.Application.Customers.WebsiteClassification;

/// <summary>
/// Classifica o `website` de um lead como site próprio vs diretório de terceiro (EXTR-03).
/// Porta 1:1 de `classify_url` / `DIRECTORY_HOSTS` em docs/email-marketing/site_check.py
/// (decisão D-08 do 07-CONTEXT.md — a lista é canônica, não reinventar).
/// Função pura: sem rede, sem I/O, sem estado. Por isso não tem interface nem é injetada.
/// </summary>
public static class WebsiteClassifier
{
    /// <summary>
    /// Páginas de terceiros que o Extrator às vezes grava como "website" do lead.
    /// Cópia verbatim de DIRECTORY_HOSTS (site_check.py, linhas 24-29).
    /// </summary>
    private static readonly string[] DirectoryHosts =
    {
        "econodata", "cliniguia", "facebook.", "instagram.", "linkedin.", "google.", "goo.gl",
        "apontador", "guiamais", "telelistas", "solutudo", "cnpj.biz", "cnpj.info", "consultacnpj",
        "empresascnpj", "casadosdados", "yelp.", "tripadvisor", "ifood", "doctoralia", "boaconsulta",
        "jusbrasil", "olx.", "mercadolivre", "shopee", "wa.me", "whatsapp.", "bit.ly", "linktr.ee",
        "youtube.", "tiktok.", "kekanto", "hotmart", "lojaintegrada", "wixsite", "site123",
        "negocio.site", "business.site", "nuvemshop", "blogspot", "wordpress.com"
    };

    /// <summary>
    /// Visão somente-leitura da lista canônica. Existe para o teste de paridade com
    /// DIRECTORY_HOSTS (site_check.py) poder assertar contagem/conteúdo sem grep frágil (D-08).
    /// </summary>
    public static IReadOnlyList<string> DirectoryHostList => DirectoryHosts;

    /// <summary>
    /// Valores placeholder que o Extrator grava quando não achou website.
    /// Espelha `if not u or u in ('-', 'n/a', 'null', 'none')` do Python.
    /// </summary>
    private static readonly HashSet<string> PlaceholderValues =
        new(StringComparer.Ordinal) { "-", "n/a", "null", "none" };

    /// <summary>
    /// Classifica a URL. NUNCA lança — entrada malformada vira <see cref="WebsiteKind.Unknown"/>.
    /// </summary>
    public static WebsiteKind Classify(string? url)
    {
        var u = (url ?? string.Empty).Trim().ToLowerInvariant();

        if (string.IsNullOrEmpty(u) || PlaceholderValues.Contains(u))
            return WebsiteKind.Unknown;

        string host;
        try
        {
            // Espelha urlparse(u if '://' in u else 'http://' + u).netloc
            var absolute = u.Contains("://", StringComparison.Ordinal) ? u : "http://" + u;
            host = new Uri(absolute, UriKind.Absolute).Host;
        }
        catch (UriFormatException)
        {
            // Python nunca lança aqui (urlparse devolve netloc vazio) — manter paridade.
            return WebsiteKind.Unknown;
        }

        if (string.IsNullOrEmpty(host) || !host.Contains('.'))
            return WebsiteKind.Unknown;

        return DirectoryHosts.Any(d => host.Contains(d, StringComparison.Ordinal))
            ? WebsiteKind.Directory
            : WebsiteKind.OwnSite;
    }
}
