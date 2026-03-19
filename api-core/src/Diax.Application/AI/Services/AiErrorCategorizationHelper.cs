using Diax.Domain.AI;
using System.Net.Http;

namespace Diax.Application.AI.Services;

/// <summary>
/// Classifies AI provider errors into structured categories for observability and diagnostics.
/// This helper is intentionally static — it has no state and just maps exception/message patterns.
/// </summary>
public static class AiErrorCategorizationHelper
{
    /// <summary>
    /// Derives an <see cref="AiErrorCategory"/> string from an exception and optional HTTP status code.
    /// </summary>
    public static string Categorize(Exception ex, int? httpStatusCode = null)
    {
        // Timeout
        if (ex is TimeoutException || ex is TaskCanceledException tce && !tce.CancellationToken.IsCancellationRequested)
            return AiErrorCategory.Timeout;

        // HTTP-status-based classification (when status code is known)
        if (httpStatusCode.HasValue)
        {
            return httpStatusCode.Value switch
            {
                401 or 403 => AiErrorCategory.AuthFailed,
                402         => AiErrorCategory.QuotaExhausted,
                404         => AiErrorCategory.ModelNotFound,
                422         => AiErrorCategory.InvalidRequest,
                429         => AiErrorCategory.RateLimit,
                >= 500      => AiErrorCategory.ProviderUnavailable,
                _           => AiErrorCategory.Unknown
            };
        }

        // Message-pattern fallback
        var msg = ex.Message.ToLowerInvariant();

        if (ContainsAny(msg,
            "quota", "credit", "insufficient_quota", "billing", "balance",
            "out of credits", "sem crédito", "quota exceeded", "limit exceeded",
            "usage limit", "current quota exceeded"))
            return AiErrorCategory.QuotaExhausted;

        if (ContainsAny(msg,
            "rate limit", "rate_limit", "too many requests", "429",
            "ratelimit", "rate limited", "throttl"))
            return AiErrorCategory.RateLimit;

        if (ContainsAny(msg,
            "unauthorized", "invalid api key", "api key", "authentication",
            "401", "403", "forbidden", "access denied", "invalid token",
            "credentials", "auth", "permission denied"))
            return AiErrorCategory.AuthFailed;

        if (ContainsAny(msg,
            "not found", "model not found", "no such model", "404",
            "deprecated", "does not exist", "unknown model",
            "invalid model", "model_not_found"))
            return AiErrorCategory.ModelNotFound;

        if (ContainsAny(msg,
            "api key não configurada", "api key inválida", "nenhum fallback configurado",
            "sem chave", "não configurada"))
            return AiErrorCategory.ConfigurationMissing;

        if (ContainsAny(msg,
            "não suporta geração", "does not support", "capability", "not supported"))
            return AiErrorCategory.CapabilityMismatch;

        if (ContainsAny(msg,
            "invalid request", "bad request", "422", "400",
            "validation", "invalid parameter", "invalid input"))
            return AiErrorCategory.InvalidRequest;

        if (ContainsAny(msg,
            "service unavailable", "503", "502", "500", "internal server error",
            "bad gateway", "upstream", "provider error", "server error",
            "temporarily unavailable", "overloaded"))
            return AiErrorCategory.ProviderUnavailable;

        if (ex is OperationCanceledException)
            return AiErrorCategory.Timeout;

        return AiErrorCategory.Unknown;
    }

    /// <summary>
    /// Extracts HTTP status code from the exception message if available.
    /// Many AI client exceptions embed "HTTP {code}" or "Status: {code}" patterns.
    /// </summary>
    public static int? ExtractHttpStatusCode(Exception ex)
    {
        var msg = ex.Message;

        // Pattern: "HTTP 429", "HTTP 403", etc.
        var httpMatch = System.Text.RegularExpressions.Regex.Match(msg, @"\bHTTP\s+(\d{3})\b");
        if (httpMatch.Success && int.TryParse(httpMatch.Groups[1].Value, out var code1))
            return code1;

        // Pattern: "Status: 429", "status code 503"
        var statusMatch = System.Text.RegularExpressions.Regex.Match(msg, @"\bstatus(?:\s+code)?[:\s]+(\d{3})\b",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (statusMatch.Success && int.TryParse(statusMatch.Groups[1].Value, out var code2))
            return code2;

        // Pattern: standalone 3-digit HTTP error codes (4xx/5xx)
        var standaloneMatch = System.Text.RegularExpressions.Regex.Match(msg, @"\b([45]\d{2})\b");
        if (standaloneMatch.Success && int.TryParse(standaloneMatch.Groups[1].Value, out var code3))
            return code3;

        return null;
    }

    /// <summary>
    /// Sanitizes an error message for logging: truncates to 500 chars and removes potential secret patterns.
    /// </summary>
    public static string SanitizeForLog(string? message, int maxLength = 500)
    {
        if (string.IsNullOrWhiteSpace(message)) return "(no message)";

        // Remove potential API key patterns (long alphanumeric strings that look like keys)
        var sanitized = System.Text.RegularExpressions.Regex.Replace(
            message,
            @"[A-Za-z0-9\-_]{32,}",
            "[REDACTED]");

        return sanitized.Length > maxLength
            ? sanitized[..maxLength] + "..."
            : sanitized;
    }

    private static bool ContainsAny(string text, params string[] patterns)
        => patterns.Any(p => text.Contains(p, StringComparison.OrdinalIgnoreCase));
}
