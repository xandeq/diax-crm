namespace Diax.Application.Customers.Services;

/// <summary>
/// Detecta domínios de e-mail placeholder / lixo ANTES de qualquer query DNS (custo zero de I/O).
/// Porta 1:1 de `is_junk_domain` em docs/email-marketing/mx_check.py.
///
/// Por que existe além da checagem de MX: `wixpress.com` e `sentry.io` são domínios REAIS com MX
/// válido — infraestrutura de terceiros que vaza como remetente default de formulário. Uma
/// checagem de MX pura NÃO pegaria esses casos. O bounce de 7,5% da semana de 03-04/09 incluiu
/// `instagram.local` e `wixpress.com`.
///
/// Um lead rejeitado por aqui conta no bucket "e-mail lixo" (LowQualityEmailRejectedCount),
/// não no bucket "sem MX" — preserva os 4 buckets da decisão D-04.
///
/// Função pura: sem rede, sem I/O, sem estado.
/// </summary>
public static class JunkDomainFilter
{
    /// <summary>Cópia verbatim de JUNK_SUFFIXES (mx_check.py linha 14).</summary>
    private static readonly string[] JunkSuffixes =
    {
        ".local", ".localhost", ".invalid", ".test", ".example", ".internal", ".lan"
    };

    /// <summary>Cópia verbatim de JUNK_HOSTS (mx_check.py linhas 15-18).</summary>
    private static readonly HashSet<string> JunkHosts = new(StringComparer.Ordinal)
    {
        "localhost", "example.com", "example.com.br", "email.com", "site.com.br", "site.com",
        "test.com", "teste.com", "teste.com.br", "dominio.com", "dominio.com.br",
        "seusite.com.br", "empresa.com.br", "google_maps", "google.com", "gmail.con",
        "hotmail.con", "wixpress.com", "sentry.io", "sentry-next.wixpress.com", "wordpress.com",
        "wix.com", "squarespace.com", "godaddy.com", "hostgator.com.br", "mailinator.com"
    };

    /// <summary>Cópia verbatim de JUNK_PARTS (mx_check.py linha 19).</summary>
    private static readonly string[] JunkParts =
    {
        "wixpress", "sentry", "placeholder", "noemail", "sememail", "nomail"
    };

    /// <summary>
    /// True se o domínio é comprovadamente inútil para entrega de e-mail.
    /// Recebe o DOMÍNIO (parte depois do '@'), não o e-mail inteiro.
    /// </summary>
    public static bool IsJunk(string? domain)
    {
        var d = (domain ?? string.Empty).Trim().ToLowerInvariant();

        if (string.IsNullOrEmpty(d))
            return true;

        // Espelha `if not d or '.' not in d and d != 'localhost' and d != 'google_maps'`.
        // (localhost e google_maps também estão em JunkHosts, então o resultado final é o mesmo,
        //  mas a estrutura é mantida idêntica ao Python para facilitar auditoria de paridade.)
        if (!d.Contains('.') && d != "localhost" && d != "google_maps")
            return true;

        if (JunkHosts.Contains(d))
            return true;

        if (JunkSuffixes.Any(s => d.EndsWith(s, StringComparison.Ordinal)))
            return true;

        return JunkParts.Any(p => d.Contains(p, StringComparison.Ordinal));
    }

    /// <summary>
    /// Conveniência: extrai o domínio de um e-mail e aplica <see cref="IsJunk(string?)"/>.
    /// E-mail nulo/vazio ou sem '@' devolve false (não é papel deste filtro rejeitar malformado —
    /// a validação de e-mail a jusante já faz isso com erro rastreável).
    /// </summary>
    public static bool IsJunkEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var at = email.LastIndexOf('@');
        if (at < 0 || at == email.Length - 1)
            return false;

        return IsJunk(email[(at + 1)..]);
    }
}
