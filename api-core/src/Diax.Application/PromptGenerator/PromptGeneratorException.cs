using System;

namespace Diax.Application.PromptGenerator;

/// <summary>
/// Exception específica para erros de integração com providers de IA.
/// Captura informações estruturadas para logging e resposta ao frontend.
/// </summary>
public class PromptGeneratorException : Exception
{
    public string Provider { get; }
    public int StatusCode { get; }
    public string ErrorMessage { get; }
    public string? RawResponse { get; }

    public PromptGeneratorException(
        string provider,
        int statusCode,
        string errorMessage,
        string? rawResponse = null,
        Exception? innerException = null)
        : base($"[{provider}] {errorMessage}", innerException)
    {
        Provider = provider;
        StatusCode = statusCode;
        ErrorMessage = errorMessage;
        RawResponse = rawResponse;
    }

    /// <summary>
    /// Mensagem segura para retornar ao frontend (sem secrets).
    /// </summary>
    public string GetSafeMessage()
    {
        return StatusCode switch
        {
            401 => $"Authentication failed with {Provider}. Please check API key configuration.",
            403 => $"Access denied by {Provider}. Check API permissions.",
            429 => $"Rate limit exceeded on {Provider}. Please try again later.",
            500 or 502 or 503 => $"{Provider} service is temporarily unavailable. Please try again.",
            408 => $"Request to {Provider} timed out. Try a shorter prompt or try again.",
            _ => $"Error from {Provider}: {ErrorMessage}"
        };
    }
}
