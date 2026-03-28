using Diax.Application.Logs;
using Diax.Domain.Logs;
using System.Diagnostics;
using DomainLogLevel = Diax.Domain.Logs.LogLevel;

namespace Diax.Api.Middleware;

/// <summary>
/// Middleware que registra todas as requisições HTTP na tabela de logs,
/// com foco especial em respostas de erro (4xx/5xx).
/// </summary>
public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
    private const long SlowRequestThresholdMs = 1500;

    public RequestResponseLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    // Paths que nunca precisam de captura de body (alto volume, sempre 2xx)
    private static readonly string[] SkipPaths = ["/health", "/email-images"];

    public async Task InvokeAsync(HttpContext context, IAppLogService appLogService)
    {
        var path = context.Request.Path.Value ?? "";
        var module = ResolveModule(path);

        // Bypass completo para paths de alta frequência sem necessidade de logging de erros
        if (Array.Exists(SkipPaths, p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
            ?? context.TraceIdentifier;

        // Captura o body da resposta apenas para capturar conteúdo de erro (4xx/5xx)
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        string? responseContent = null;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            // Lê o conteúdo da resposta se for erro
            if (context.Response.StatusCode >= 400)
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                responseContent = await new StreamReader(responseBody).ReadToEndAsync();
                responseBody.Seek(0, SeekOrigin.Begin);
            }
            else
            {
                responseBody.Seek(0, SeekOrigin.Begin);
            }

            // Copia a resposta de volta para o stream original
            await responseBody.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream;
            context.Response.Headers["X-Response-Time-Ms"] = stopwatch.ElapsedMilliseconds.ToString();
            context.Response.Headers["X-App-Module"] = module;

            if (stopwatch.ElapsedMilliseconds >= SlowRequestThresholdMs)
            {
                _logger.LogWarning(
                    "Slow request detected: {Method} {Path} [{Module}] -> {StatusCode} in {ElapsedMilliseconds}ms",
                    context.Request.Method,
                    context.Request.Path,
                    module,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds);
            }

            // Loga requisições com erro (4xx/5xx) na tabela app_logs,
            // desde que ainda não tenham sido logadas pelo ExceptionLoggingMiddleware
            if (context.Response.StatusCode >= 400 && !context.Items.ContainsKey("IsLogged"))
            {
                await LogRequestAsync(context, appLogService, correlationId, stopwatch.ElapsedMilliseconds, responseContent, module);
            }
        }
    }

    private async Task LogRequestAsync(
        HttpContext context,
        IAppLogService appLogService,
        string correlationId,
        long responseTimeMs,
        string? responseContent,
        string module)
    {
        try
        {
            var statusCode = context.Response.StatusCode;
            var level = GetLogLevel(statusCode);
            var category = LogCategory.Integration;

            // Extrai informações do erro da resposta
            var errorMessage = ExtractErrorMessage(responseContent, statusCode);

            // Extrai headers relevantes (sem dados sensíveis)
            var headers = context.Request.Headers
                .Where(h => !IsSensitiveHeader(h.Key))
                .ToDictionary(h => h.Key, h => h.Value.ToString());

            var headersJson = System.Text.Json.JsonSerializer.Serialize(headers);

            // Dados adicionais
            var additionalData = new Dictionary<string, object?>
            {
                ["responseBody"] = responseContent?.Length > 2000
                    ? responseContent.Substring(0, 2000) + "..."
                    : responseContent,
                ["responseTimeMs"] = responseTimeMs,
                ["module"] = module,
                ["userAgent"] = context.Request.Headers.UserAgent.ToString(),
                ["referer"] = context.Request.Headers.Referer.ToString()
            };

            var additionalDataJson = System.Text.Json.JsonSerializer.Serialize(additionalData);

            await appLogService.LogAsync(
                level: level,
                category: category,
                message: errorMessage,
                correlationId: correlationId,
                userId: context.User?.FindFirst("sub")?.Value
                    ?? context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                requestPath: context.Request.Path,
                queryString: context.Request.QueryString.ToString(),
                httpMethod: context.Request.Method,
                statusCode: statusCode,
                headersJson: headersJson,
                clientIp: context.Connection.RemoteIpAddress?.ToString(),
                userAgent: context.Request.Headers.UserAgent.ToString(),
                additionalData: additionalDataJson,
                responseTimeMs: (int)responseTimeMs);

            _logger.LogDebug(
                "Request logged: {Method} {Path} - {StatusCode} ({ResponseTime}ms)",
                context.Request.Method,
                context.Request.Path,
                statusCode,
                responseTimeMs);
        }
        catch (Exception ex)
        {
            // Não deixa erros de logging interromperem a aplicação
            _logger.LogError(ex, "Falha ao registrar log de requisição");
        }
    }

    private static DomainLogLevel GetLogLevel(int statusCode)
    {
        return statusCode switch
        {
            >= 500 => DomainLogLevel.Error,
            >= 400 => DomainLogLevel.Warning,
            _ => DomainLogLevel.Information
        };
    }

    private static string ExtractErrorMessage(string? responseContent, int statusCode)
    {
        if (string.IsNullOrEmpty(responseContent))
        {
            return $"HTTP {statusCode} - {GetStatusCodeDescription(statusCode)}";
        }

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(responseContent);
            var root = doc.RootElement;

            // Tenta extrair mensagem de erro de diferentes formatos
            if (root.TryGetProperty("message", out var message))
                return message.GetString() ?? $"HTTP {statusCode}";

            if (root.TryGetProperty("Message", out var messageUpper))
                return messageUpper.GetString() ?? $"HTTP {statusCode}";

            if (root.TryGetProperty("error", out var error))
            {
                if (error.ValueKind == System.Text.Json.JsonValueKind.String)
                    return error.GetString() ?? $"HTTP {statusCode}";
                if (error.TryGetProperty("message", out var errorMsg))
                    return errorMsg.GetString() ?? $"HTTP {statusCode}";
            }

            if (root.TryGetProperty("title", out var title))
                return title.GetString() ?? $"HTTP {statusCode}";

            if (root.TryGetProperty("detail", out var detail))
                return detail.GetString() ?? $"HTTP {statusCode}";

            // Para ValidationProblemDetails
            if (root.TryGetProperty("errors", out var errors))
            {
                var errorMessages = new List<string>();
                foreach (var prop in errors.EnumerateObject())
                {
                    foreach (var err in prop.Value.EnumerateArray())
                    {
                        errorMessages.Add($"{prop.Name}: {err.GetString()}");
                    }
                }
                if (errorMessages.Count > 0)
                    return string.Join("; ", errorMessages);
            }
        }
        catch
        {
            // Se não for JSON válido, usa o conteúdo truncado
            return responseContent.Length > 200
                ? responseContent.Substring(0, 200) + "..."
                : responseContent;
        }

        return $"HTTP {statusCode} - {GetStatusCodeDescription(statusCode)}";
    }

    private static string GetStatusCodeDescription(int statusCode)
    {
        return statusCode switch
        {
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            405 => "Method Not Allowed",
            409 => "Conflict",
            422 => "Unprocessable Entity",
            429 => "Too Many Requests",
            500 => "Internal Server Error",
            502 => "Bad Gateway",
            503 => "Service Unavailable",
            504 => "Gateway Timeout",
            _ => "Error"
        };
    }

    private static bool IsSensitiveHeader(string headerName)
    {
        var sensitiveHeaders = new[]
        {
            "Authorization",
            "Cookie",
            "Set-Cookie",
            "X-Api-Key",
            "X-Auth-Token"
        };

        return sensitiveHeaders.Contains(headerName, StringComparer.OrdinalIgnoreCase);
    }

    public static string ResolveModule(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "unknown";
        }

        var normalizedPath = path.Trim().ToLowerInvariant();

        if (normalizedPath.Contains("/auth")) return "auth";
        if (normalizedPath.Contains("/personal-control") || normalizedPath.Contains("/finance")) return "finance";
        if (normalizedPath.Contains("/customers")) return "customers";
        if (normalizedPath.Contains("/leads")) return "leads";
        if (normalizedPath.Contains("/email-campaigns") || normalizedPath.Contains("/campaigns")) return "campaigns";
        if (normalizedPath.Contains("/ai")) return "ai";
        if (normalizedPath.Contains("/apikeys")) return "api-keys";
        if (normalizedPath.Contains("/users")) return "users";
        if (normalizedPath.Contains("/logs")) return "logs";
        if (normalizedPath.Contains("/audit")) return "audit";

        return "core";
    }
}

public static class RequestResponseLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestResponseLoggingMiddleware>();
    }
}
