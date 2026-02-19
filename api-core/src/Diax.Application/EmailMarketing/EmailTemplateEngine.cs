using System.Text.RegularExpressions;

namespace Diax.Application.EmailMarketing;

public partial class EmailTemplateEngine : IEmailTemplateEngine
{
    [GeneratedRegex("\\{\\{\\s*(?<key>[a-zA-Z0-9_]+)\\s*\\}\\}", RegexOptions.Compiled)]
    private static partial Regex TokenRegex();

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
            if (lookup.TryGetValue(key, out var value))
            {
                return value ?? string.Empty;
            }

            return match.Value;
        });
    }
}
