using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace Diax.Api.Middleware;

/// <summary>
/// O que pode ir para app_logs/error_logs. Headers por lista de PERMITIDOS: integrações usam
/// chaves em headers próprios (X-Integration-Key, X-Alert-Api-Key, svix-*, X-Brevo-Signature…)
/// e uma lista de bloqueio sempre esquece a próxima. Query string: valores de parâmetros com
/// cara de segredo viram "***" (o webhook Brevo aceita ?secret=).
/// </summary>
public static partial class LogRedaction
{
    private static readonly HashSet<string> SafeHeaderNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Accept", "Accept-Encoding", "Accept-Language", "Content-Type", "Content-Length",
        "Host", "Origin", "Referer", "User-Agent", "X-Correlation-Id", "X-Request-Id",
        "X-Forwarded-For", "X-Forwarded-Proto", "X-Original-URL", "Sec-Fetch-Site",
        "Sec-Fetch-Mode", "Sec-Fetch-Dest", "Sec-Ch-Ua", "Sec-Ch-Ua-Mobile", "Sec-Ch-Ua-Platform"
    };

    [GeneratedRegex(@"(?i)(^|[?&])([^=&]*(secret|token|key|sig|signature|password|pwd|auth)[^=&]*)=([^&]*)")]
    private static partial Regex SecretQueryParam();

    public static Dictionary<string, string> SafeHeaders(IHeaderDictionary headers) =>
        headers.Where(h => SafeHeaderNames.Contains(h.Key))
               .ToDictionary(h => h.Key, h => h.Value.ToString());

    public static string SafeQuery(string? queryString) =>
        string.IsNullOrEmpty(queryString)
            ? queryString ?? string.Empty
            : SecretQueryParam().Replace(queryString, m => $"{m.Groups[1].Value}{m.Groups[2].Value}=***");
}
