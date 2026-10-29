namespace Diax.Application.EmailMarketing;

/// <summary>
/// Cabeçalhos de descadastro one-click (RFC 8058) exigidos por Gmail/Yahoo para envio em massa:
/// <c>List-Unsubscribe: &lt;https://...&gt;</c> + <c>List-Unsubscribe-Post: List-Unsubscribe=One-Click</c>.
/// </summary>
public static class ListUnsubscribeHeaders
{
    public const string ListUnsubscribe = "List-Unsubscribe";
    public const string ListUnsubscribePost = "List-Unsubscribe-Post";

    /// <summary>Retorna null se a URL não for https absoluta (a RFC exige URL segura; não enviamos header quebrado).</summary>
    public static Dictionary<string, string>? Build(string? unsubscribeUrl)
    {
        if (string.IsNullOrWhiteSpace(unsubscribeUrl)
            || !Uri.TryCreate(unsubscribeUrl, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps
            || unsubscribeUrl.Any(c => c is '<' or '>' or '\r' or '\n'))
            return null;

        return new Dictionary<string, string>
        {
            [ListUnsubscribe] = $"<{unsubscribeUrl}>",
            [ListUnsubscribePost] = "List-Unsubscribe=One-Click",
        };
    }
}
