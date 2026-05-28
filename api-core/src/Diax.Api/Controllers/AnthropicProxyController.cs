using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers;

/// <summary>
/// Proxy transparente para a Anthropic Messages API.
///
/// Permite que clientes em redes restritas (ex: notebooks corporativos que bloqueiam
/// api.anthropic.com) usem o SDK oficial da Anthropic apontando para este servidor.
///
/// Auth: header X-Api-Key com a ServiceApiKey configurada no servidor.
///
/// Uso no Python SDK:
///   client = anthropic.Anthropic(
///       api_key="ignored",
///       base_url="https://api.alexandrequeiroz.com.br/proxy",
///       default_headers={"X-Api-Key": "SERVICE_API_KEY"},
///   )
/// </summary>
[ApiController]
[Route("proxy")]
[Authorize]
public class AnthropicProxyController : ControllerBase
{
    private const string AnthropicBaseUrl = "https://api.anthropic.com";
    private const string AnthropicVersion = "2023-06-01";

    private static readonly string[] _headersToForward =
    [
        "anthropic-version",
        "anthropic-beta",
        "content-type",
    ];

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AnthropicProxyController> _logger;

    public AnthropicProxyController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<AnthropicProxyController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Proxy catch-all: encaminha qualquer POST de /proxy/v1/{path} para api.anthropic.com/v1/{path}.
    /// Suporta tanto respostas normais quanto streaming SSE (stream: true no body).
    /// </summary>
    [HttpPost("v1/{**path}")]
    public async Task ProxyPostAsync(string path, CancellationToken cancellationToken)
    {
        var anthropicKey = _configuration["ANTHROPIC_API_KEY"]
            ?? _configuration["Anthropic:ApiKey"]
            ?? _configuration["PromptGenerator:Anthropic:ApiKey"];

        if (string.IsNullOrWhiteSpace(anthropicKey))
        {
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await Response.WriteAsJsonAsync(new { error = "Anthropic API key not configured on proxy server." }, cancellationToken);
            return;
        }

        // Lê o body inteiro da requisição
        Request.EnableBuffering();
        using var bodyReader = new StreamReader(Request.Body, leaveOpen: true);
        var bodyText = await bodyReader.ReadToEndAsync(cancellationToken);

        // Detecta se a requisição quer streaming
        var isStream = bodyText.Contains("\"stream\":true", StringComparison.Ordinal)
                    || bodyText.Contains("\"stream\": true", StringComparison.Ordinal);

        var targetUrl = $"{AnthropicBaseUrl}/v1/{path}";

        _logger.LogInformation(
            "[AnthropicProxy] {Method} {Path} stream={Stream} from {IP}",
            Request.Method, path, isStream, HttpContext.Connection.RemoteIpAddress);

        using var httpClient = _httpClientFactory.CreateClient("anthropic-proxy");

        using var upstream = new HttpRequestMessage(HttpMethod.Post, targetUrl)
        {
            Content = new StringContent(bodyText, System.Text.Encoding.UTF8, "application/json")
        };

        // Headers obrigatórios da Anthropic API
        upstream.Headers.Add("x-api-key", anthropicKey);
        upstream.Headers.Add("anthropic-version", AnthropicVersion);

        // Repassa headers opcionais do cliente (ex: anthropic-beta para extended thinking)
        foreach (var header in _headersToForward)
        {
            if (header.Equals("content-type", StringComparison.OrdinalIgnoreCase)) continue; // já definido no Content
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
            _logger.LogError(ex, "[AnthropicProxy] Falha ao contactar Anthropic");
            Response.StatusCode = StatusCodes.Status502BadGateway;
            await Response.WriteAsJsonAsync(new { error = $"Proxy error: {ex.Message}" }, CancellationToken.None);
            return;
        }

        Response.StatusCode = (int)upstreamResponse.StatusCode;

        // Propaga content-type da resposta upstream
        if (upstreamResponse.Content.Headers.ContentType is not null)
            Response.ContentType = upstreamResponse.Content.Headers.ContentType.ToString();

        if (isStream && upstreamResponse.IsSuccessStatusCode)
        {
            // SSE: desabilita buffering e transmite chunks em tempo real
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
                _logger.LogWarning(ex, "[AnthropicProxy] Erro durante stream SSE");
            }
        }
        else
        {
            // Resposta normal (ou erro): envia body completo
            var responseBody = await upstreamResponse.Content.ReadAsByteArrayAsync(cancellationToken);
            await Response.Body.WriteAsync(responseBody, cancellationToken);
        }

        upstreamResponse.Dispose();
    }
}
