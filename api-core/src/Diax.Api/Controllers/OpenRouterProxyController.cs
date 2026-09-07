using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers;

/// <summary>
/// Proxy transparente para a OpenRouter API (compatível com o formato OpenAI).
///
/// Mesmo propósito do <see cref="AnthropicProxyController"/>: permite que clientes em redes
/// restritas usem SDKs apontando para este servidor. A diferença é o upstream — OpenRouter dá
/// acesso a dezenas de modelos (incluindo gratuitos) por trás de uma API única.
///
/// Auth: header X-Api-Key com a ServiceApiKey do servidor (mesmo esquema do proxy da Anthropic).
///
/// Uso no SDK da OpenAI (Python):
///   client = OpenAI(
///       api_key="ignored",
///       base_url="https://api.alexandrequeiroz.com.br/openrouter/v1",
///       default_headers={"X-Api-Key": "SERVICE_API_KEY"},
///   )
///   client.chat.completions.create(model="meta-llama/llama-3.1-8b-instruct:free", messages=[...])
///
/// Ou via curl:
///   curl https://api.alexandrequeiroz.com.br/openrouter/v1/chat/completions \
///     -H "X-Api-Key: SERVICE_API_KEY" -H "Content-Type: application/json" \
///     -d '{"model":"...","messages":[{"role":"user","content":"oi"}]}'
/// </summary>
[ApiController]
[Route("openrouter")]
[Authorize]
public class OpenRouterProxyController : ControllerBase
{
    private const string OpenRouterBaseUrl = "https://openrouter.ai/api";

    /// <summary>
    /// Headers opcionais do cliente que são repassados ao upstream.
    /// `http-referer` e `x-title` são o mecanismo de atribuição da OpenRouter (aparecem no
    /// ranking público de apps); `content-type` é tratado pelo próprio Content da requisição.
    /// </summary>
    private static readonly string[] _headersToForward =
    [
        "http-referer",
        "x-title",
    ];

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenRouterProxyController> _logger;

    public OpenRouterProxyController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<OpenRouterProxyController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Proxy catch-all: encaminha qualquer POST de /openrouter/v1/{path} para
    /// openrouter.ai/api/v1/{path}. Suporta resposta normal e streaming SSE (stream: true).
    /// </summary>
    [HttpPost("v1/{**path}")]
    public async Task ProxyPostAsync(string path, CancellationToken cancellationToken)
    {
        var openRouterKey = _configuration["OPENROUTER_API_KEY"]
            ?? _configuration["OpenRouter:ApiKey"]
            ?? _configuration["PromptGenerator:OpenRouter:ApiKey"];

        if (string.IsNullOrWhiteSpace(openRouterKey))
        {
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await Response.WriteAsJsonAsync(new { error = "OpenRouter API key not configured on proxy server." }, cancellationToken);
            return;
        }

        Request.EnableBuffering();
        using var bodyReader = new StreamReader(Request.Body, leaveOpen: true);
        var bodyText = await bodyReader.ReadToEndAsync(cancellationToken);

        var isStream = bodyText.Contains("\"stream\":true", StringComparison.Ordinal)
                    || bodyText.Contains("\"stream\": true", StringComparison.Ordinal);

        var targetUrl = $"{OpenRouterBaseUrl}/v1/{path}";

        _logger.LogInformation(
            "[OpenRouterProxy] {Method} {Path} stream={Stream} from {IP}",
            Request.Method, path, isStream, HttpContext.Connection.RemoteIpAddress);

        using var httpClient = _httpClientFactory.CreateClient("openrouter-proxy");

        using var upstream = new HttpRequestMessage(HttpMethod.Post, targetUrl)
        {
            Content = new StringContent(bodyText, System.Text.Encoding.UTF8, "application/json")
        };

        // OpenRouter usa Bearer token (formato OpenAI), diferente do x-api-key da Anthropic.
        upstream.Headers.Authorization = new AuthenticationHeaderValue("Bearer", openRouterKey);

        foreach (var header in _headersToForward)
        {
            if (Request.Headers.TryGetValue(header, out var val) && !string.IsNullOrWhiteSpace(val))
                upstream.Headers.TryAddWithoutValidation(header, (string?)val);
        }

        if (isStream)
            upstream.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        HttpResponseMessage? upstreamResponse = null;
        try
        {
            upstreamResponse = await httpClient.SendAsync(
                upstream,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            upstreamResponse?.Dispose();
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[OpenRouterProxy] Falha ao contactar OpenRouter");
            Response.StatusCode = StatusCodes.Status502BadGateway;
            await Response.WriteAsJsonAsync(new { error = $"Proxy error: {ex.Message}" }, CancellationToken.None);
            return;
        }

        Response.StatusCode = (int)upstreamResponse.StatusCode;

        if (upstreamResponse.Content.Headers.ContentType is not null)
            Response.ContentType = upstreamResponse.Content.Headers.ContentType.ToString();

        if (isStream && upstreamResponse.IsSuccessStatusCode)
        {
            Response.Headers["Cache-Control"] = "no-cache, no-transform";
            Response.Headers["X-Accel-Buffering"] = "no";
            Response.Headers["Connection"] = "keep-alive";
            HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

            try
            {
                await upstreamResponse.Content.CopyToAsync(Response.Body, cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
            catch (OperationCanceledException) { /* cliente desconectou */ }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[OpenRouterProxy] Erro durante stream SSE");
            }
        }
        else
        {
            var responseBody = await upstreamResponse.Content.ReadAsByteArrayAsync(cancellationToken);
            await Response.Body.WriteAsync(responseBody, cancellationToken);
        }

        upstreamResponse.Dispose();
    }

    /// <summary>
    /// Proxy de leitura: encaminha GET de /openrouter/v1/{path} para openrouter.ai/api/v1/{path}.
    /// Existe porque a OpenRouter expõe endpoints úteis por GET que a Anthropic não tem —
    /// notadamente `/v1/models` (catálogo, para descobrir quais modelos gratuitos estão vivos)
    /// e `/v1/credits` (saldo da conta).
    /// </summary>
    [HttpGet("v1/{**path}")]
    public async Task ProxyGetAsync(string path, CancellationToken cancellationToken)
    {
        var openRouterKey = _configuration["OPENROUTER_API_KEY"]
            ?? _configuration["OpenRouter:ApiKey"]
            ?? _configuration["PromptGenerator:OpenRouter:ApiKey"];

        if (string.IsNullOrWhiteSpace(openRouterKey))
        {
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await Response.WriteAsJsonAsync(new { error = "OpenRouter API key not configured on proxy server." }, cancellationToken);
            return;
        }

        var targetUrl = $"{OpenRouterBaseUrl}/v1/{path}";
        if (Request.QueryString.HasValue)
            targetUrl += Request.QueryString.Value;

        _logger.LogInformation(
            "[OpenRouterProxy] GET {Path} from {IP}", path, HttpContext.Connection.RemoteIpAddress);

        using var httpClient = _httpClientFactory.CreateClient("openrouter-proxy");
        using var upstream = new HttpRequestMessage(HttpMethod.Get, targetUrl);
        upstream.Headers.Authorization = new AuthenticationHeaderValue("Bearer", openRouterKey);

        HttpResponseMessage? upstreamResponse = null;
        try
        {
            upstreamResponse = await httpClient.SendAsync(upstream, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            upstreamResponse?.Dispose();
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[OpenRouterProxy] Falha ao contactar OpenRouter (GET)");
            Response.StatusCode = StatusCodes.Status502BadGateway;
            await Response.WriteAsJsonAsync(new { error = $"Proxy error: {ex.Message}" }, CancellationToken.None);
            return;
        }

        Response.StatusCode = (int)upstreamResponse.StatusCode;

        if (upstreamResponse.Content.Headers.ContentType is not null)
            Response.ContentType = upstreamResponse.Content.Headers.ContentType.ToString();

        var responseBody = await upstreamResponse.Content.ReadAsByteArrayAsync(cancellationToken);
        await Response.Body.WriteAsync(responseBody, cancellationToken);

        upstreamResponse.Dispose();
    }
}
