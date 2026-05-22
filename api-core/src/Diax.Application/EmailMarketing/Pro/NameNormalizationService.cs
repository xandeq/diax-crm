using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Diax.Domain.Customers.Enums;

namespace Diax.Application.EmailMarketing.Pro;

public partial class NameNormalizationService : INameNormalizationService
{
    [GeneratedRegex(@"[\(\[\{].*?[\)\]\}]", RegexOptions.Compiled)]
    private static partial Regex ParenRegex();

    [GeneratedRegex(@"^[\d\W]+$", RegexOptions.Compiled)]
    private static partial Regex AllNonWordOrDigitRegex();

    private static readonly HashSet<string> JunkWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "ltda", "me", "eireli", "sa", "epp", "mei", "empresa", "comercial", "servicos",
        "services", "solucoes", "solutions", "group", "grupo", "industria", "industrias",
        "construtora", "engenharia", "consultoria", "assessoria", "representacoes",
        "distribuidora", "clinica", "odonto", "farma", "med", "saude", "fitness",
        "academia", "imobiliaria", "imoveis", "negocios", "digital", "tech",
        "tecnologia", "informatica", "sistemas", "software", "web", "online",
        "brasil", "brazil", "transportadora", "transportadoras", "logistica", "log",
        "express", "expresso", "cargas", "frete", "transporte", "transportes",
        "grafica", "grafico", "papelaria", "padaria", "pousada", "restaurante",
        "lanchonete", "mercado", "supermercado", "farmacia", "laboratorio",
        "escritorio", "contabilidade", "contabil", "advocacia", "advocacias",
        "otica", "oticas", "joalheria", "relojoaria", "moda", "boutique",
        "eletrica", "hidraulica", "construcao", "reformas",
        "outlook", "gmail", "hotmail", "yahoo", "bol", "uol", "ig", "terra",
        "admin", "info", "contato", "contact", "suporte", "support", "vendas",
        "sales", "comercial", "marketing", "noreply", "no-reply",
    };

    // Backward-compat: returns only the first valid token (firstName)
    public string NormalizeName(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        raw = ParenRegex().Replace(raw, "").Trim();
        raw = raw.Trim('.', ',', ';', ':', '-', '_', '@', '#', '/', ' ');

        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var tokens = raw.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        foreach (var token in tokens)
        {
            var clean = token.Trim('.', ',', ';', ':');

            if (clean.Length < 2) continue;
            if (clean.Contains('@') || clean.Contains('.')) continue;
            if (AllNonWordOrDigitRegex().IsMatch(clean)) continue;

            var ascii = RemoveDiacritics(clean);
            if (JunkWords.Contains(ascii)) continue;

            var result = char.ToUpperInvariant(clean[0]) + clean[1..].ToLowerInvariant();
            if (result.Length >= 2)
                return result;
        }

        return string.Empty;
    }

    public NameNormalizationResult Normalize(string? raw, string? email = null)
    {
        var fromName = NormalizeFullNameInternal(raw);

        if (fromName.Score >= 75)
            return fromName;

        if (!string.IsNullOrWhiteSpace(email) && email.Contains('@'))
        {
            var fromEmail = ExtractFromEmailPrefix(email);
            if (fromEmail.Score >= 55)
                return fromEmail;

            // Fallback: extract from domain when email prefix is generic/junk
            var fromDomain = ExtractFromDomain(email);
            if (fromDomain.Score > 0)
            {
                var best = fromEmail.Score >= fromDomain.Score ? fromEmail : fromDomain;
                if (best.Score > fromName.Score)
                    return best;
            }
            else if (fromEmail.Score > fromName.Score)
            {
                return fromEmail;
            }
        }

        return fromName;
    }

    private NameNormalizationResult NormalizeFullNameInternal(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new("", 0, NormalizationSource.Deterministic);

        raw = ParenRegex().Replace(raw, "").Trim();
        raw = raw.Trim('.', ',', ';', ':', '-', '_', '@', '#', '/', ' ');

        if (string.IsNullOrWhiteSpace(raw))
            return new("", 0, NormalizationSource.Deterministic);

        var tokens = raw.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var validTokens = new List<string>();

        foreach (var token in tokens)
        {
            var clean = token.Trim('.', ',', ';', ':');

            if (clean.Length < 2) continue;
            if (clean.Contains('@') || clean.Contains('.')) continue;
            if (AllNonWordOrDigitRegex().IsMatch(clean)) continue;

            var ascii = RemoveDiacritics(clean);
            if (JunkWords.Contains(ascii)) continue;

            var normalized = char.ToUpperInvariant(clean[0]) + clean[1..].ToLowerInvariant();
            if (normalized.Length >= 2)
                validTokens.Add(normalized);
        }

        if (validTokens.Count == 0)
            return new("", 0, NormalizationSource.Deterministic);

        var result = string.Join(" ", validTokens);
        var score = validTokens.Count switch
        {
            1 => 75,
            2 => 88,
            _ => 95
        };

        return new(result, score, NormalizationSource.Deterministic);
    }

    private NameNormalizationResult ExtractFromEmailPrefix(string email)
    {
        var localPart = email.Split('@')[0];

        var plusIdx = localPart.IndexOf('+');
        if (plusIdx > 0) localPart = localPart[..plusIdx];

        var parts = localPart.Split('.', '_', '-');
        var validParts = new List<string>();

        foreach (var part in parts)
        {
            if (part.Length < 2) continue;
            if (part.All(char.IsDigit)) continue;
            if (AllNonWordOrDigitRegex().IsMatch(part)) continue;

            var ascii = RemoveDiacritics(part);
            if (JunkWords.Contains(ascii)) continue;

            var normalized = char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant();
            if (normalized.Length >= 2)
                validParts.Add(normalized);
        }

        if (validParts.Count == 0)
            return new("", 0, NormalizationSource.EmailPrefix);

        var result = string.Join(" ", validParts);
        var score = validParts.Count switch
        {
            1 => 55,
            2 => 68,
            _ => 72
        };

        return new(result, score, NormalizationSource.EmailPrefix);
    }

    private NameNormalizationResult ExtractFromDomain(string email)
    {
        if (!email.Contains('@'))
            return new("", 0, NormalizationSource.Domain);

        var domain = email.Split('@')[1].ToLowerInvariant();
        var parts = domain.Split('.');

        // Pick the second-level domain (SLD): pvbeauty from pvbeauty.com.br
        string sld;
        if (parts.Length >= 3 && parts[^1].Length == 2) // ccTLD like .br, .uk
            sld = parts[^3];
        else if (parts.Length >= 2)
            sld = parts[^2];
        else
            return new("", 0, NormalizationSource.Domain);

        if (sld.Length < 3)
            return new("", 0, NormalizationSource.Domain);

        var ascii = RemoveDiacritics(sld);
        if (JunkWords.Contains(ascii))
            return new("", 0, NormalizationSource.Domain);

        // Split compound domain names on known word boundaries using common suffixes
        // e.g. "beoadvogados" -> "Beo Advogados", "escolamobile" -> "Escola Mobile"
        var expanded = ExpandDomainName(sld);
        var normalized = char.ToUpperInvariant(expanded[0]) + expanded[1..].ToLowerInvariant();

        // Score: 50 for single word, 58 for expanded compound
        var score = expanded.Contains(' ') ? 58 : 50;
        return new(normalized, score, NormalizationSource.Domain);
    }

    // Common Brazilian business suffix words used in domains — longest first to avoid partial matches
    private static readonly string[] DomainSuffixes =
    [
        "advogados", "contabil", "contabilidade", "informatica", "imoveis",
        "imobiliaria", "construtora", "engenharia", "consultoria", "assessoria",
        "pinturas", "blindagens", "distribuidora", "distribuidoras",
        "pneus", "tintas", "moveis", "eletrica", "hidraulica",
        "servicos", "solucoes", "sistemas", "tecnologia", "digital",
        "academia", "fitness", "saude", "clinica", "odonto", "farma",
        "restaurante", "churrasco", "padaria", "mercado", "supermercado",
        "escola", "colegio", "cursos", "educacao", "treinamentos",
        "hotel", "pousada", "turismo", "viagens", "eventos",
        "logistica", "transporte", "transportes", "express", "expresso",
        "grafica", "papelaria", "brindes", "uniforme", "uniformes",
        "mall", "center", "centre", "shop", "store", "loja", "lojas",
        "group", "grupo", "holding", "network", "connect",
        "beauty", "beleza", "estetica", "cabelo", "barbearia",
        "auto", "oficina", "mecanica", "automotivo", "pecas",
        "solar", "energia", "agro", "fazenda", "rural",
        "med", "medica", "medico", "laboratorio", "exames",
        "adv", "law", "juridico", "legal",
        "arq", "arquitetura", "design", "criativo", "agencia",
    ];

    private static string ExpandDomainName(string sld)
    {
        // Try to split off a known suffix from the end
        foreach (var suffix in DomainSuffixes)
        {
            if (sld.Length > suffix.Length + 2 && sld.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                var prefix = sld[..^suffix.Length];
                if (prefix.Length >= 2)
                {
                    var prefixTitle = char.ToUpperInvariant(prefix[0]) + prefix[1..].ToLowerInvariant();
                    var suffixTitle = char.ToUpperInvariant(suffix[0]) + suffix[1..].ToLowerInvariant();
                    return $"{prefixTitle} {suffixTitle}";
                }
            }
        }

        // No known suffix found — return as-is (single title-cased word)
        return sld;
    }

    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                builder.Append(c);
        }
        return builder.ToString();
    }
}
