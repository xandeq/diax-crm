using Diax.Application.Common;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Diax.Application.PromptGenerator;

public class PromptGeneratorService : IApplicationService, IPromptGeneratorService
{
    private const string DefaultProvider = "chatgpt";
    private const int DefaultTimeoutSeconds = 30;

    private readonly ILogger<PromptGeneratorService> _logger;
    private readonly PromptGeneratorSettings _settings;

    public PromptGeneratorService(
        ILogger<PromptGeneratorService> logger,
        PromptGeneratorSettings settings)
    {
        _logger = logger;
        _settings = settings;
    }

    public async Task<string> GenerateAsync(string rawPrompt, string provider)
    {
        if (string.IsNullOrWhiteSpace(rawPrompt))
        {
            throw new ArgumentException("Prompt não pode ser vazio.", nameof(rawPrompt));
        }

        var normalizedProvider = NormalizeProvider(provider);
        var settings = GetProviderSettings(normalizedProvider);

        _logger.LogInformation("Prompt generation started. Provider: {Provider}. RawPromptLength: {Length}",
            normalizedProvider, rawPrompt.Length);

        var metaPrompt = BuildMetaPrompt();
        var finalPrompt = await SendPromptAsync(settings, metaPrompt, rawPrompt);

        _logger.LogInformation("Prompt generation completed. Provider: {Provider}.", normalizedProvider);

        return finalPrompt;
    }

    private async Task<string> SendPromptAsync(ProviderSettings settings, string metaPrompt, string rawPrompt)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException($"API key not configured for provider '{settings.ProviderName}'.");
        }

        var endpoint = BuildEndpoint(settings.BaseUrl);
        var payload = new
        {
            model = settings.Model,
            messages = new[]
            {
                new { role = "system", content = metaPrompt },
                new { role = "user", content = rawPrompt }
            },
            temperature = 0.2
        };

        var timeoutSeconds = GetTimeoutSeconds();
        using var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(timeoutSeconds)
        };
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);

        using var response = await client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Prompt generation failed. Provider: {Provider}. Status: {StatusCode}.",
                settings.ProviderName, (int)response.StatusCode);
            throw new InvalidOperationException("Falha ao gerar prompt. Tente novamente.");
        }

        var content = ExtractContent(responseBody);
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("Resposta inválida do provedor de IA.");
        }

        return content.Trim();
    }

    private string NormalizeProvider(string provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            return DefaultProvider;
        }

        return provider.Trim().ToLowerInvariant();
    }

    private ProviderSettings GetProviderSettings(string provider)
    {
        return provider switch
        {
            "perplexity" => BuildSettings(
                "perplexity",
                _settings.Perplexity,
                "https://api.perplexity.ai",
                "sonar-pro"),
            "deepseek" => BuildSettings(
                "deepseek",
                _settings.DeepSeek,
                "https://api.deepseek.com",
                "deepseek-chat"),
            _ => BuildSettings(
                "chatgpt",
                _settings.OpenAI,
                "https://api.openai.com/v1",
                "gpt-4o-mini")
        };
    }

    private int GetTimeoutSeconds()
    {
        return _settings.TimeoutSeconds > 0
            ? Math.Clamp(_settings.TimeoutSeconds, 5, 120)
            : DefaultTimeoutSeconds;
    }

    private ProviderSettings BuildSettings(string providerName, ProviderConfig config, string defaultBaseUrl, string defaultModel)
    {
        var baseUrl = string.IsNullOrWhiteSpace(config.BaseUrl) ? defaultBaseUrl : config.BaseUrl;
        var model = string.IsNullOrWhiteSpace(config.Model) ? defaultModel : config.Model;
        return new ProviderSettings(providerName, config.ApiKey, baseUrl, model);
    }

    private string BuildEndpoint(string baseUrl)
    {
        var cleanBase = string.IsNullOrWhiteSpace(baseUrl) ? "" : baseUrl.TrimEnd('/');
        return string.IsNullOrWhiteSpace(cleanBase)
            ? "chat/completions"
            : $"{cleanBase}/chat/completions";
    }

    private string ExtractContent(string responseBody)
    {
        try
        {
            using var document = JsonDocument.Parse(responseBody);
            var root = document.RootElement;

            if (root.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array && choices.GetArrayLength() > 0)
            {
                var choice = choices[0];

                if (choice.TryGetProperty("message", out var message) && message.TryGetProperty("content", out var content))
                {
                    return content.GetString() ?? string.Empty;
                }

                if (choice.TryGetProperty("text", out var text))
                {
                    return text.GetString() ?? string.Empty;
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI provider response.");
        }

        return string.Empty;
    }

    private string BuildMetaPrompt()
    {
        return """
Você é um especialista em Prompt Engineering.
Sua tarefa é transformar o prompt fornecido pelo usuário em um prompt profissional, claro, estruturado e altamente eficaz.

Siga obrigatoriamente estas regras:
- Seja educado e profissional
- Use verbos de ação claros
- Organize o prompt em seções
- Declare contexto, objetivo e público-alvo
- Sugira encadeamento de prompts quando aplicável
- Não responda à tarefa, apenas gere o prompt final

Estrutura obrigatória:
Contexto
Objetivo
Público-alvo
Instruções principais
Regras e restrições
Formato da resposta esperada
Sugestão de próximos prompts (se fizer sentido)
""";
    }

    private sealed record ProviderSettings(
        string ProviderName,
        string? ApiKey,
        string BaseUrl,
        string Model);
}
