using System.Security.Claims;
using System.Text.Json;
using Diax.Application.Logs;
using Diax.Domain.Logs;
using Serilog;

namespace Diax.Api.Middleware;

public class ExceptionLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IWebHostEnvironment _environment;

    private static readonly HashSet<string> SensitiveHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization", "Cookie", "X-Api-Key", "X-Auth-Token"
    };

    public ExceptionLoggingMiddleware(
        RequestDelegate next, 
        IServiceScopeFactory scopeFactory,
        IWebHostEnvironment environment)
    {
        _next = next;
        _scopeFactory = scopeFactory;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        Log.Error(exception, "Unhandled exception occurred. Path: {Path}, Method: {Method}",
            context.Request.Path, context.Request.Method);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var logService = scope.ServiceProvider.GetRequiredService<AppLogService>();

            var appLog = AppLog.FromException(
                exception,
                requestId: context.TraceIdentifier,
                correlationId: context.Items["CorrelationId"]?.ToString(),
                userId: context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                requestPath: context.Request.Path,
                httpMethod: context.Request.Method,
                clientIp: context.Connection.RemoteIpAddress?.ToString(),
                userAgent: context.Request.Headers.UserAgent.ToString(),
                headersJson: SerializeHeaders(context.Request.Headers),
                queryString: context.Request.QueryString.ToString(),
                environment: _environment.EnvironmentName);

            await logService.CreateAsync(appLog);
        }
        catch (Exception logEx)
        {
            Log.Error(logEx, "Failed to save exception log to database");
        }

        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var response = new
        {
            message = "An unexpected error occurred",
            code = "INTERNAL_ERROR",
            correlationId = context.Items["CorrelationId"]?.ToString(),
            requestId = context.TraceIdentifier
        };

        await context.Response.WriteAsJsonAsync(response);
    }

    private static string SerializeHeaders(IHeaderDictionary headers)
    {
        var filtered = headers
            .Where(h => !SensitiveHeaders.Contains(h.Key))
            .ToDictionary(h => h.Key, h => h.Value.ToString());

        return JsonSerializer.Serialize(filtered);
    }
}

public static class ExceptionLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionLoggingMiddleware>();
    }
}
