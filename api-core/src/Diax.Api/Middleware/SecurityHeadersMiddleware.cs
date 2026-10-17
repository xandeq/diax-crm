namespace Diax.Api.Middleware;

/// <summary>
/// Headers de segurança. A API só devolve JSON e imagens de email — nunca HTML pra navegador —
/// então a CSP pode ser fechada. Fora de produção a CSP fica de fora pra não quebrar o Swagger UI.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly bool _strictCsp;

    public SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment environment)
    {
        _next = next;
        _strictCsp = environment.IsProduction();
    }

    public Task InvokeAsync(HttpContext context)
    {
        Apply(context.Response.Headers, _strictCsp);
        return _next(context);
    }

    public static void Apply(IHeaderDictionary headers, bool strictCsp)
    {
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        if (strictCsp)
            headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
    }
}
