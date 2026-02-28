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
                return value;
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
