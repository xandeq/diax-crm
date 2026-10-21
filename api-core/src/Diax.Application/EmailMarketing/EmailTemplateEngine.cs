using System.Text.RegularExpressions;

namespace Diax.Application.EmailMarketing;

public partial class EmailTemplateEngine : IEmailTemplateEngine
{
    [GeneratedRegex("\\{\\{\\s*(?<key>[a-zA-Z0-9_]+)\\s*\\}\\}", RegexOptions.Compiled)]
    private static partial Regex TokenRegex();

    /// <summary>
    /// Fallbacks padrão para variáveis comuns quando o valor está vazio.
    /// </summary>
    private static readonly Dictionary<string, string> DefaultFallbacks = new(StringComparer.OrdinalIgnoreCase)
    {
        ["nome"] = "Cliente",
        ["firstName"] = "Cliente",
        ["name"] = "Cliente",
        ["empresa"] = "sua empresa",
        ["company"] = "sua empresa",
        ["companyName"] = "sua empresa",
        ["email"] = "",
        ["website"] = "",
        ["leadStatus"] = "Lead",
        ["status"] = "Lead",
    };

    public string Render(string template, IReadOnlyDictionary<string, string?> variables)
        => RenderCore(template, variables, v => v);

    // Valores vêm de terceiros (Google Maps/Extrator): nunca entram crus no HTML.
    // Escape mínimo (sem virar acento em &#233;) — vale dentro de texto e de atributo.
    public string RenderHtml(string template, IReadOnlyDictionary<string, string?> variables)
        => RenderCore(template, variables, EscapeHtml);

    private static string EscapeHtml(string value) => value
        .Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
        .Replace("\"", "&quot;").Replace("'", "&#39;");

    private static string RenderCore(string template, IReadOnlyDictionary<string, string?> variables, Func<string, string> encode)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return string.Empty;
        }

        var lookup = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in variables)
        {
            lookup[pair.Key] = pair.Value;
        }

        return TokenRegex().Replace(template, match =>
        {
            var key = match.Groups["key"].Value;

            // Tenta buscar o valor fornecido
            if (lookup.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return encode(value);
            }

            // Se vazio ou não existe, tenta usar fallback
            if (DefaultFallbacks.TryGetValue(key, out var fallback))
            {
                return fallback;
            }

            // Se não tem fallback, remove o token (retorna vazio)
            return string.Empty;
        });
    }
}
