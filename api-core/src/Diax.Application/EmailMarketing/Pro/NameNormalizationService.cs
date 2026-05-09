using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

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
        "outlook", "gmail", "hotmail", "yahoo", "bol", "uol", "ig", "terra"
    };

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
