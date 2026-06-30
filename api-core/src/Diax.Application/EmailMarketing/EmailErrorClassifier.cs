namespace Diax.Application.EmailMarketing;

/// <summary>
/// Predicados compartilhados para classificação de erros de ESP.
/// Extraído aqui para ser reutilizado por PilotCircuitBreaker e EmailProviderCircuitBreaker.
/// </summary>
public static class EmailErrorClassifier
{
    public static bool IsCriticalAuthError(string? errorMsg)
    {
        if (string.IsNullOrEmpty(errorMsg)) return false;
        return errorMsg.Contains("bounce", StringComparison.OrdinalIgnoreCase)
            || errorMsg.Contains("unauthorized", StringComparison.OrdinalIgnoreCase)
            || errorMsg.Contains("autenticação", StringComparison.OrdinalIgnoreCase)
            || errorMsg.Contains("authentication", StringComparison.OrdinalIgnoreCase)
            || errorMsg.Contains("401", StringComparison.OrdinalIgnoreCase)
            || errorMsg.Contains("invalid api key", StringComparison.OrdinalIgnoreCase)
            || errorMsg.Contains("apikey", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsTransientRateLimit(string? errorMsg)
    {
        if (string.IsNullOrEmpty(errorMsg)) return false;
        return errorMsg.Contains("429", StringComparison.OrdinalIgnoreCase)
            || errorMsg.Contains("too many requests", StringComparison.OrdinalIgnoreCase)
            || errorMsg.Contains("rate limit", StringComparison.OrdinalIgnoreCase)
            || errorMsg.Contains("rate_limit", StringComparison.OrdinalIgnoreCase)
            || errorMsg.Contains("ratelimit", StringComparison.OrdinalIgnoreCase)
            || errorMsg.Contains("too_many_requests", StringComparison.OrdinalIgnoreCase);
    }

    // Rate-limit puro (sem sinal de auth) é transitório — não conta para a janela de erro
    public static bool IsIgnorable(string? errorMsg)
        => IsTransientRateLimit(errorMsg) && !IsCriticalAuthError(errorMsg);
}
