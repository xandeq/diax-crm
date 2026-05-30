using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Diax.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
[Authorize]
public class StatusController : BaseApiController
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<StatusController> _logger;

    private static readonly ServiceDefinition[] Services =
    [
        new("DIAX CRM API",       "https://api.alexandrequeiroz.com.br/health", "SaaS",          "api.alexandrequeiroz.com.br"),
        new("DIAX CRM",           "https://crm.alexandrequeiroz.com.br",         "SaaS",          "crm.alexandrequeiroz.com.br"),
        new("InvestIQ",           "https://investiq.com.br",                     "SaaS",          "investiq.com.br"),
        new("InvestIQ API",       "https://api.investiq.com.br/health",          "SaaS",          "api.investiq.com.br"),
        new("ClearDesk",          "https://cleardesk.com.br",                    "SaaS",          "cleardesk.com.br"),
        new("ClearDesk API",      "https://api.cleardesk.com.br/health",         "SaaS",          "api.cleardesk.com.br"),
        new("VagaNaGringa",       "https://vaganagringa.dev",                    "SaaS",          "vaganagringa.dev"),
        new("VagaNaGringa API",   "https://api.vaganagringa.dev/health",         "SaaS",          "api.vaganagringa.dev"),
        new("Extrator de Dados",  "https://extratordedados.com.br",             "SaaS",          "extratordedados.com.br"),
        new("Tarefista",          "https://tarefista.com.br",                    "SaaS",          "tarefista.com.br"),
        new("AleCook",            "https://alecook.com.br",                      "SaaS",          "alecook.com.br"),
        new("Bairronow",          "https://bairronow.com.br",                    "SaaS",          "bairronow.com.br"),
        new("Website AQ",         "https://alexandrequeiroz.com.br",             "Website",       "alexandrequeiroz.com.br"),
        new("Fera do Prompt",     "https://feradoprompt.com.br",                 "SaaS",          "feradoprompt.com.br"),
    ];

    public StatusController(IHttpClientFactory httpClientFactory, ILogger<StatusController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpGet("services")]
    public async Task<IActionResult> GetServicesStatus(CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(8);

        var tasks = Services.Select(svc => CheckServiceAsync(client, svc, ct));
        var results = await Task.WhenAll(tasks);

        return Ok(new
        {
            checkedAt = DateTime.UtcNow,
            services = results
        });
    }

    private async Task<object> CheckServiceAsync(HttpClient client, ServiceDefinition svc, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Head, svc.Url);
            request.Headers.UserAgent.ParseAdd("DiaxCRM-StatusCheck/1.0");

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            sw.Stop();

            var statusCode = (int)response.StatusCode;
            var online = statusCode < 500;

            return new
            {
                svc.Name,
                svc.Url,
                svc.Domain,
                svc.Category,
                online,
                statusCode,
                responseTimeMs = (int)sw.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogDebug("Status check failed for {Service}: {Error}", svc.Name, ex.Message);
            return new
            {
                svc.Name,
                svc.Url,
                svc.Domain,
                svc.Category,
                online = false,
                statusCode = 0,
                responseTimeMs = (int)sw.ElapsedMilliseconds
            };
        }
    }

    private record ServiceDefinition(string Name, string Url, string Category, string Domain);
}
