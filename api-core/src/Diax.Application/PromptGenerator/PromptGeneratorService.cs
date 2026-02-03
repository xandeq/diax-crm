using Diax.Application.Common;
using Diax.Application.PromptGenerator.Common;
using Diax.Application.PromptGenerator.Dtos;
using Diax.Domain.Common;
using Diax.Domain.PromptGenerator;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Diax.Application.PromptGenerator;

public class PromptGeneratorService : IApplicationService, IPromptGeneratorService
{
    private const string DefaultProvider = "chatgpt";
    private const string DefaultPromptType = "professional";
    private const int DefaultTimeoutSeconds = 30;

    private readonly ILogger<PromptGeneratorService> _logger;
    private readonly PromptGeneratorSettings _settings;
    private readonly IUserPromptRepository _userPromptRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PromptGeneratorService(
        ILogger<PromptGeneratorService> logger,
        PromptGeneratorSettings settings,
        IUserPromptRepository userPromptRepository,
        IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _settings = settings;
        _userPromptRepository = userPromptRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<string> GenerateAsync(string rawPrompt, string provider, string promptType, string? model = null)
    {
        if (string.IsNullOrWhiteSpace(rawPrompt))
        {
            throw new ArgumentException("Prompt não pode ser vazio.", nameof(rawPrompt));
        }

        var normalizedProvider = NormalizeProvider(provider);
        var normalizedPromptType = NormalizePromptType(promptType);
        var settings = GetProviderSettings(normalizedProvider, model);

        _logger.LogInformation("Prompt generation started. Provider: {Provider}. Model: {Model}. PromptType: {PromptType}. RawPromptLength: {Length}",
            normalizedProvider, settings.Model, normalizedPromptType, rawPrompt.Length);

        var metaPrompt = BuildMetaPrompt(normalizedPromptType);

        var finalPrompt = normalizedProvider == "gemini"
            ? await SendGeminiPromptAsync(settings, metaPrompt, rawPrompt)
            : await SendPromptAsync(settings, metaPrompt, rawPrompt);

        _logger.LogInformation("Prompt generation completed. Provider: {Provider}. Model: {Model}. PromptType: {PromptType}.",
            normalizedProvider, settings.Model, normalizedPromptType);

        return finalPrompt;
    }

    private async Task<string> SendGeminiPromptAsync(ProviderSettings settings, string metaPrompt, string rawPrompt)
    {
        // ===== VALIDAÇÃO ESPECÍFICA DO GEMINI =====
        var (isValid, errorMessage) = AiModelCatalog.ValidateGeminiModel(settings.Model);
        if (!isValid)
        {
            _logger.LogWarning("Invalid Gemini model requested: {Model}. Error: {Error}", settings.Model, errorMessage);
            throw new ArgumentException(errorMessage);
        }

        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            _logger.LogError("API Key missing (Gemini detected as potentially unconfigured). Check environment variables.");
            throw new InvalidOperationException($"API key not configured for provider '{settings.ProviderName}'.");
        }

        // Gemini URL: https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={API_KEY}
        // Note: setting.Model already starts with 'models/' from catalog
        var baseUrl = settings.BaseUrl.TrimEnd('/');
        var endpoint = $"{baseUrl}/{settings.Model}:generateContent?key={settings.ApiKey}";

        var payload = new
        {
            system_instruction = new
            {
                parts = new[] { new { text = metaPrompt } }
            },
            contents = new[]
            {
                new { role = "user", parts = new[] { new { text = rawPrompt } } }
            },
            generationConfig = new
            {
                temperature = 0.2
            }
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

        using var response = await client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Gemini prompt generation failed. Provider: {Provider}. Status: {StatusCode}. Body: {Body}",
                settings.ProviderName, (int)response.StatusCode, responseBody);
            throw new InvalidOperationException($"Erro no Gemini ({(int)response.StatusCode}): {SanitizeError(responseBody)}");
        }

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            var root = document.RootElement;

            if (root.TryGetProperty("candidates", out var candidates) && candidates.ValueKind == JsonValueKind.Array && candidates.GetArrayLength() > 0)
            {
                var candidate = candidates[0];
                if (candidate.TryGetProperty("content", out var content) && content.TryGetProperty("parts", out var parts) && parts.ValueKind == JsonValueKind.Array && parts.GetArrayLength() > 0)
                {
                    return parts[0].GetProperty("text").GetString() ?? string.Empty;
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error parsing Gemini response");
        }

        throw new InvalidOperationException("Resposta inválida do Google Gemini.");
    }

    private async Task<string> SendPromptAsync(ProviderSettings settings, string metaPrompt, string rawPrompt)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            _logger.LogError(
                "API Key missing for provider {Provider}. Configured providers: OpenAI={HasOpenAI}, Perplexity={HasPerplexity}, DeepSeek={HasDeepSeek}, Gemini={HasGemini}, OpenRouter={HasOpenRouter}",
                settings.ProviderName,
                !string.IsNullOrWhiteSpace(_settings.OpenAI.ApiKey),
                !string.IsNullOrWhiteSpace(_settings.Perplexity.ApiKey),
                !string.IsNullOrWhiteSpace(_settings.DeepSeek.ApiKey),
                !string.IsNullOrWhiteSpace(_settings.Gemini.ApiKey),
                !string.IsNullOrWhiteSpace(_settings.OpenRouter.ApiKey));

            throw new InvalidOperationException($"API key not configured for provider '{settings.ProviderName}'. Please check environment variables or appsettings.");
        }

        var endpoint = BuildEndpoint(settings.BaseUrl);

        _logger.LogDebug(
            "Preparing request to {Provider}. Endpoint: {Endpoint}. Model: {Model}",
            settings.ProviderName, endpoint, settings.Model);

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

        try
        {
            using var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(timeoutSeconds)
            };

            var jsonPayload = JsonSerializer.Serialize(payload);

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);

            if (settings.ProviderName == "openrouter")
            {
                request.Headers.Add("HTTP-Referer", "https://crm.alexandrequeiroz.com.br");
                request.Headers.Add("X-Title", "DIAX CRM");
            }

            _logger.LogDebug(
                "Sending request to {Provider}. Endpoint: {Endpoint}. PayloadSize: {Size} bytes",
                settings.ProviderName, endpoint, jsonPayload.Length);

            using var response = await client.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Provider {Provider} returned error. Endpoint: {Endpoint}. Status: {StatusCode}. ResponseBody: {Body}",
                    settings.ProviderName,
                    endpoint,
                    (int)response.StatusCode,
                    TruncateForLog(responseBody, 1000));

                var errorMessage = ExtractErrorMessage(responseBody, settings.ProviderName);
                throw new PromptGeneratorException(
                    settings.ProviderName,
                    (int)response.StatusCode,
                    errorMessage,
                    responseBody);
            }

            var content = ExtractContent(responseBody);
            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning(
                    "Provider {Provider} returned empty content. ResponseBody: {Body}",
                    settings.ProviderName,
                    TruncateForLog(responseBody, 500));

                throw new InvalidOperationException($"Provider '{settings.ProviderName}' returned empty response.");
            }

            _logger.LogInformation(
                "Provider {Provider} succeeded. Model: {Model}. ResponseLength: {Length}",
                settings.ProviderName, settings.Model, content.Length);

            return content.Trim();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "HTTP error calling {Provider}. Endpoint: {Endpoint}. Message: {Message}",
                settings.ProviderName, endpoint, ex.Message);

            throw new PromptGeneratorException(
                settings.ProviderName,
                (int?)ex.StatusCode ?? 0,
                $"Connection error: {ex.Message}",
                null,
                ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex,
                "Timeout calling {Provider} after {Timeout}s. Endpoint: {Endpoint}",
                settings.ProviderName, timeoutSeconds, endpoint);

            throw new PromptGeneratorException(
                settings.ProviderName,
                408,
                $"Request timeout after {timeoutSeconds} seconds.",
                null,
                ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex,
                "Request cancelled for {Provider}. Endpoint: {Endpoint}",
                settings.ProviderName, endpoint);

            throw new PromptGeneratorException(
                settings.ProviderName,
                0,
                "Request was cancelled.",
                null,
                ex);
        }
    }

    private static string TruncateForLog(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return "[empty]";
        return text.Length <= maxLength ? text : text.Substring(0, maxLength) + "...[truncated]";
    }

    private string ExtractErrorMessage(string responseBody, string provider)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            // OpenRouter format
            if (root.TryGetProperty("error", out var error))
            {
                if (error.ValueKind == JsonValueKind.Object)
                {
                    if (error.TryGetProperty("message", out var msg))
                        return msg.GetString() ?? "Unknown error";
                    if (error.TryGetProperty("code", out var code))
                        return code.GetString() ?? "Unknown error";
                }
                if (error.ValueKind == JsonValueKind.String)
                    return error.GetString() ?? "Unknown error";
            }

            // Perplexity/DeepSeek format
            if (root.TryGetProperty("message", out var message))
                return message.GetString() ?? "Unknown error";

            // Fallback
            if (root.TryGetProperty("detail", out var detail))
                return detail.GetString() ?? "Unknown error";
        }
        catch (JsonException)
        {
            // Not JSON
        }

        return responseBody.Length > 200 ? responseBody.Substring(0, 200) + "..." : responseBody;
    }

    private string NormalizeProvider(string provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            return DefaultProvider;
        }

        var normalized = provider.Trim().ToLowerInvariant();

        // Mapear aliases conhecidos para nomes internos
        return normalized switch
        {
            "google_gemini" => "gemini",
            "google" => "gemini",
            "openai" => "chatgpt",
            "gpt" => "chatgpt",
            _ => normalized
        };
    }

    private string SanitizeError(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var error))
            {
                if (error.ValueKind == JsonValueKind.String) return error.GetString() ?? body;
                if (error.ValueKind == JsonValueKind.Object && error.TryGetProperty("message", out var msg)) return msg.GetString() ?? body;
            }
        }
        catch { /* ignore parse error */ }

        return body.Length > 200 ? body.Substring(0, 200) + "..." : body;
    }

    private string NormalizePromptType(string promptType)
    {
        if (string.IsNullOrWhiteSpace(promptType))
        {
            return DefaultPromptType;
        }

        return promptType.Trim().ToLowerInvariant();
    }

    private ProviderSettings GetProviderSettings(string provider, string? model)
    {
        return provider switch
        {
            "openrouter" => BuildSettings(
                "openrouter",
                _settings.OpenRouter,
                "https://openrouter.ai/api/v1",
                model),
            "gemini" => BuildSettings(
                "gemini",
                _settings.Gemini,
                "https://generativelanguage.googleapis.com/v1beta",
                model),
            "perplexity" => BuildSettings(
                "perplexity",
                _settings.Perplexity,
                "https://api.perplexity.ai",
                model),
            "deepseek" => BuildSettings(
                "deepseek",
                _settings.DeepSeek,
                "https://api.deepseek.com",
                model),
            _ => BuildSettings(
                "chatgpt",
                _settings.OpenAI,
                "https://api.openai.com/v1",
                model)
        };
    }

    private int GetTimeoutSeconds()
    {
        return _settings.TimeoutSeconds > 0
            ? Math.Clamp(_settings.TimeoutSeconds, 5, 120)
            : DefaultTimeoutSeconds;
    }

    private ProviderSettings BuildSettings(string providerName, ProviderConfig config, string defaultBaseUrl, string? requestedModel)
    {
        var baseUrl = string.IsNullOrWhiteSpace(config.BaseUrl) ? defaultBaseUrl : config.BaseUrl;

        // Priority:
        // 1. Requested model from frontend (if valid)
        // 2. Configured model in appsettings
        // 3. Catalog default model

        string model;
        var catalogDefault = AiModelCatalog.GetDefaultModel(providerName);

        if (!string.IsNullOrWhiteSpace(requestedModel))
        {
            if (AiModelCatalog.IsModelValid(providerName, requestedModel))
            {
                model = requestedModel;
            }
            else
            {
                _logger.LogWarning("Invalid model '{RequestedModel}' for provider '{Provider}'. Falling back to default.", requestedModel, providerName);
                model = !string.IsNullOrWhiteSpace(config.Model) ? config.Model : catalogDefault;
            }
        }
        else
        {
            model = !string.IsNullOrWhiteSpace(config.Model) ? config.Model : catalogDefault;
        }

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

    private string BuildMetaPrompt(string promptType)
    {
        return promptType switch
        {
            "pas" => BuildPasMetaPrompt(),
            "aida" => BuildAidaMetaPrompt(),
            "fab" => BuildFabMetaPrompt(),
            "pear" => BuildPearMetaPrompt(),
            "goat" => BuildGoatMetaPrompt(),
            "care" => BuildCareMetaPrompt(),
            "rtf" => BuildRtfMetaPrompt(),
            "risen" => BuildRisenMetaPrompt(),
            "costar" => BuildCostarMetaPrompt(),
            "cot" => BuildCotMetaPrompt(),
            "tot" => BuildTotMetaPrompt(),
            "cod" => BuildCodMetaPrompt(),
            "tag" => BuildTagMetaPrompt(),
            "bab" => BuildBabMetaPrompt(),
            "create" => BuildCreateMetaPrompt(),
            "fsp" => BuildFspMetaPrompt(),
            "sref" => BuildSrefMetaPrompt(),
            "deep_research" => BuildDeepResearchMetaPrompt(),
            "context_objective" => BuildContextObjectiveMetaPrompt(),
            _ => BuildProfessionalMetaPrompt()
        };
    }

    private string BuildProfessionalMetaPrompt()
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

    private string BuildPasMetaPrompt()
    {
        return """
Você é um especialista em copywriting e Prompt Engineering usando a técnica P.A.S. (Problema, Agitar, Solução).

Sua tarefa é transformar o prompt do usuário em um prompt estruturado seguindo a técnica P.A.S.:

PROBLEMA: Identifique claramente o problema ou dor que o público enfrenta.
AGITAR: Aprofunde-se no problema, mostrando as consequências negativas e o impacto emocional.
SOLUÇÃO: Apresente a solução de forma clara e convincente.

Regras obrigatórias:
- Seja persuasivo e emocional
- Use linguagem que ressoe com o público-alvo
- Crie urgência e desejo de ação
- Não responda à tarefa, apenas gere o prompt final estruturado

Estrutura obrigatória do prompt gerado:
PROBLEMA: [Descrição clara do problema]
AGITAR: [Aprofundamento nas consequências e dor]
SOLUÇÃO: [Apresentação da solução]
CHAMADA PARA AÇÃO: [Próximo passo sugerido]
""";
    }

    private string BuildAidaMetaPrompt()
    {
        return """
Você é um especialista em marketing e Prompt Engineering usando a técnica A.I.D.A. (Atenção, Interesse, Desejo, Ação).

Sua tarefa é transformar o prompt do usuário em um prompt estruturado seguindo a técnica A.I.D.A.:

ATENÇÃO: Capture a atenção com algo impactante ou surpreendente.
INTERESSE: Construa interesse explicando benefícios e relevância.
DESEJO: Crie desejo pintando um quadro vívido do resultado desejado.
AÇÃO: Force uma ação clara e imediata.

Regras obrigatórias:
- Use gatilhos de atenção poderosos
- Construa uma progressão lógica
- Crie conexão emocional
- Termine com CTA claro
- Não responda à tarefa, apenas gere o prompt final estruturado

Estrutura obrigatória do prompt gerado:
ATENÇÃO: [Gancho inicial impactante]
INTERESSE: [Desenvolvimento do interesse]
DESEJO: [Criação do desejo]
AÇÃO: [Chamada para ação clara]
""";
    }

    private string BuildFabMetaPrompt()
    {
        return """
Você é um especialista em descrição de produtos e Prompt Engineering usando a técnica F.A.B. (Características, Vantagens, Benefícios).

Sua tarefa é transformar o prompt do usuário em um prompt estruturado seguindo a técnica F.A.B.:

CARACTERÍSTICAS (Features): O que o produto/serviço É - especificações técnicas.
VANTAGENS (Advantages): O que ele FAZ - funcionalidades práticas.
BENEFÍCIOS (Benefits): O que isso SIGNIFICA para o usuário - valor real.

Regras obrigatórias:
- Conecte características técnicas a benefícios emocionais
- Traduza especificações em valor do mundo real
- Use linguagem clara e acessível
- Não responda à tarefa, apenas gere o prompt final estruturado

Estrutura obrigatória do prompt gerado:
CARACTERÍSTICAS: [Especificações e features]
VANTAGENS: [O que cada característica faz na prática]
BENEFÍCIOS: [Impacto real na vida do usuário]
PROPOSTA DE VALOR: [Resumo do valor único]
""";
    }

    private string BuildPearMetaPrompt()
    {
        return """
Você é um especialista em pesquisa e análise usando a técnica P.E.A.R. (Pesquisa, Extrair, Aplicar, Entregar).

Sua tarefa é transformar o prompt do usuário em um prompt estruturado seguindo a técnica P.E.A.R.:

PESQUISA (Research): Reúna informações relevantes sobre o tema.
EXTRAIR (Extract): Identifique insights-chave e padrões.
APLICAR (Apply): Aplique os insights ao contexto específico.
ENTREGAR (Deliver): Apresente resultados acionáveis.

Regras obrigatórias:
- Seja sistemático e analítico
- Foque em insights acionáveis
- Sintetize informações de forma clara
- Não responda à tarefa, apenas gere o prompt final estruturado

Estrutura obrigatória do prompt gerado:
PESQUISA: [O que precisa ser investigado]
EXTRAIR: [Insights e padrões a identificar]
APLICAR: [Como aplicar ao contexto específico]
ENTREGAR: [Formato do resultado final esperado]
""";
    }

    private string BuildGoatMetaPrompt()
    {
        return """
Você é um especialista em storytelling e Prompt Engineering usando a técnica G.O.A.T. (Objetivo, Obstáculo, Ação, Transformação).

Sua tarefa é transformar o prompt do usuário em um prompt estruturado seguindo a técnica G.O.A.T.:

OBJETIVO (Goal): Onde você quer chegar - a meta desejada.
OBSTÁCULO (Obstacle): O que está bloqueando - desafios e barreiras.
AÇÃO (Action): As etapas para superar - estratégias e táticas.
TRANSFORMAÇÃO (Transformation): O resultado final - a mudança alcançada.

Regras obrigatórias:
- Crie uma narrativa envolvente
- Mostre a jornada completa
- Use elementos emocionais
- Não responda à tarefa, apenas gere o prompt final estruturado

Estrutura obrigatória do prompt gerado:
OBJETIVO: [Meta clara e específica]
OBSTÁCULO: [Desafios e barreiras]
AÇÃO: [Passos e estratégias]
TRANSFORMAÇÃO: [Resultado e impacto final]
""";
    }

    private string BuildCareMetaPrompt()
    {
        return """
Você é um especialista em depoimentos e histórias de sucesso usando a técnica C.A.R.E. (Conteúdo, Ação, Resultado, Emoção).

Sua tarefa é transformar o prompt do usuário em um prompt estruturado seguindo a técnica C.A.R.E.:

CONTEÚDO (Content): A situação ou contexto inicial.
AÇÃO (Action): As ações específicas tomadas.
RESULTADO (Result): Os resultados mensuráveis alcançados.
EMOÇÃO (Emotion): O impacto emocional e humano.

Regras obrigatórias:
- Equilibre dados com conexão humana
- Use números e métricas quando possível
- Termine com impacto emocional forte
- Não responda à tarefa, apenas gere o prompt final estruturado

Estrutura obrigatória do prompt gerado:
CONTEÚDO: [Situação inicial e contexto]
AÇÃO: [Ações específicas realizadas]
RESULTADO: [Métricas e resultados concretos]
EMOÇÃO: [Impacto emocional e transformação pessoal]
""";
    }

    private string BuildRtfMetaPrompt()
    {
        return """
Você é um especialista em Prompt Engineering usando a técnica R.T.F. (Role, Task, Format).

Sua tarefa é transformar o prompt do usuário em um prompt estruturado, claro e direto seguindo a técnica R.T.F.:

ROLE (Papel): Defina claramente quem a IA deve ser - o papel ou persona.
TASK (Tarefa): Especifique exatamente o que precisa ser feito.
FORMAT (Formato): Indique o formato exato da resposta esperada.

Regras obrigatórias:
- Seja extremamente direto e objetivo
- Elimine ambiguidades
- Use verbos de ação claros
- Especifique formato de saída (lista, tabela, checklist, etc.)
- Não responda à tarefa, apenas gere o prompt final estruturado

Estrutura obrigatória do prompt gerado:
PAPEL: Você é [definição clara do papel].
TAREFA: [Ação específica e objetiva].
FORMATO: [Formato exato da resposta - lista numerada, tabela, checklist, etc.].
""";
    }

    private string BuildRisenMetaPrompt()
    {
        return """
Você é um especialista em Prompt Engineering usando a técnica R.I.S.E.N. (Role, Instructions, Steps, End Goal, Narrowing).

Sua tarefa é transformar o prompt do usuário em um prompt estruturado e complexo seguindo a técnica R.I.S.E.N.:

ROLE (Papel): Defina o papel especializado que a IA deve assumir.
INSTRUCTIONS (Instruções): Forneça instruções claras e detalhadas.
STEPS (Etapas): Quebre a tarefa em etapas ou fases lógicas.
END GOAL (Objetivo Final): Especifique o resultado final desejado.
NARROWING (Restrições): Defina limites claros (tamanho, tom, linguagem, escopo).

Regras obrigatórias:
- Crie planos estruturados e detalhados
- Pense em múltiplas etapas ou fases
- Estabeleça restrições claras para controlar a saída
- Não responda à tarefa, apenas gere o prompt final estruturado

Estrutura obrigatória do prompt gerado:
PAPEL: Atue como [papel especializado].
INSTRUÇÕES: [Orientações detalhadas].
ETAPAS: [Quebra em passos lógicos - pensar passo a passo].
OBJETIVO FINAL: [Meta específica e mensurável].
RESTRIÇÕES: [Limites claros - tamanho, tom, linguagem, formato].
""";
    }

    private string BuildCostarMetaPrompt()
    {
        return """
Você é um especialista em comunicação profissional e Prompt Engineering usando a técnica C.O.S.T.A.R. (Context, Objective, Style, Tone, Audience, Response).

Sua tarefa é transformar o prompt do usuário em um prompt estruturado de alta qualidade seguindo a técnica C.O.S.T.A.R.:

CONTEXT (Contexto): A situação ou cenário completo.
OBJECTIVE (Objetivo): O que precisa ser alcançado.
STYLE (Estilo): O estilo de comunicação (consultivo, executivo, técnico, etc.).
TONE (Tom): O tom emocional (empático, formal, casual, urgente, etc.).
AUDIENCE (Público): Quem vai receber a mensagem.
RESPONSE (Resposta): O formato final esperado.

Regras obrigatórias:
- Personalize para o público específico
- Ajuste estilo e tom adequadamente
- Crie conteúdo profissional e bem escrito
- Não responda à tarefa, apenas gere o prompt final estruturado

Estrutura obrigatória do prompt gerado:
CONTEXTO: [Situação completa e relevante].
OBJETIVO: [O que precisa ser alcançado].
ESTILO: [Estilo de comunicação apropriado].
TOM: [Tom emocional adequado].
PÚBLICO: [Perfil detalhado do destinatário].
FORMATO: [Tipo de resposta esperada - email, proposta, relatório, etc.].
""";
    }

    private string BuildCotMetaPrompt()
    {
        return """
Você é um especialista em raciocínio analítico usando a técnica Chain of Thought (Cadeia de Pensamento).

Sua tarefa é transformar o prompt do usuário em um prompt que force a IA a EXPLICAR SEU RACIOCÍNIO PASSO A PASSO antes de chegar à conclusão final.

Princípios da Chain of Thought:
- A IA deve "pensar em voz alta"
- Cada etapa do raciocínio deve ser explícita
- Justificar cada decisão ou conclusão parcial
- Só apresentar a resposta final após todo o raciocínio

Regras obrigatórias:
- Exija que a IA mostre o processo de pensamento
- Use frases como "Explique seu raciocínio passo a passo", "Pense em voz alta", "Justifique cada etapa"
- Apropriado para análises, diagnósticos, scoring, avaliações
- Não responda à tarefa, apenas gere o prompt final estruturado

Estrutura obrigatória do prompt gerado:
[Descrição da tarefa ou problema]

ANTES DE RESPONDER:
1. Analise passo a passo
2. Explique seu raciocínio para cada etapa
3. Justifique suas conclusões parciais
4. Só então apresente a resposta final

Mostre todo o seu processo de pensamento.
""";
    }

    private string BuildTotMetaPrompt()
    {
        return """
Você é um especialista em pensamento estratégico usando a técnica Tree of Thoughts (Árvore de Pensamentos).

Sua tarefa é transformar o prompt do usuário em um prompt que force a IA a GERAR MÚTIPLAS SOLUÇÕES, AVALIAR CADA UMA e RECOMENDAR A MELHOR.

Princípios do Tree of Thoughts:
- Gerar pelo menos 3 caminhos/soluções diferentes
- Avaliar prós e contras de cada opção
- Comparar as alternativas
- Recomendar a melhor com justificativa

Regras obrigatórias:
- Exija múltiplas opções (geralmente 3)
- Peça avaliação de vantagens e desvantagens
- Solicite comparação entre as alternativas
- Exija recomendação final justificada
- Não responda à tarefa, apenas gere o prompt final estruturado

Estrutura obrigatória do prompt gerado:
[Descrição do problema ou decisão]

Gere 3 estratégias/soluções diferentes para [problema].

Para cada opção:
1. Descreva a abordagem
2. Liste os prós
3. Liste os contras
4. Avalie a viabilidade

Então, compare as 3 opções e recomende a melhor, justificando sua escolha.
""";
    }

    private string BuildCodMetaPrompt()
    {
        return """
Você é um especialista em sumarização usando a técnica Chain of Density (Cadeia de Densidade).

Sua tarefa é transformar o prompt do usuário em um prompt que gere resumos progressivamente mais DENSOS, mantendo o mesmo tamanho, mas incorporando cada vez mais informações relevantes.

Princípios da Chain of Density:
- Resumo inicial captura os pontos principais
- Iterações subsequentes identificam informações importantes faltantes
- Cada iteração torna o resumo mais denso SEM aumentar o tamanho
- Nunca perde entidades críticas ou detalhes importantes
- Mantém legibilidade e coerência

Regras obrigatórias:
- Exija que a IA mantenha o tamanho do resumo constante
- Peça identificação de informações faltantes importantes
- Solicite compressão progressiva sem perda de contexto
- Ideal para históricos longos: tickets, emails, timelines
- Não responda à tarefa, apenas gere o prompt final estruturado

Estrutura obrigatória do prompt gerado:
Resuma o seguinte conteúdo em [N] parágrafos/palavras.

A cada iteração:
1. Identifique informações importantes que estão faltando
2. Incorpore essas informações sem aumentar o tamanho
3. Mantenha todas as entidades críticas (nomes, datas, números, decisões)
4. Garanta legibilidade

Entregue o resumo final mais denso possível.
""";
    }

    private string BuildTagMetaPrompt()
    {
        return """
Você é um especialista em Prompt Engineering usando a técnica T.A.G. (Task, Action, Goal).

Sua tarefa é transformar o prompt do usuário em um prompt estruturado focado em RESULTADO e OBJETIVO DE NEGÓCIO, seguindo a técnica T.A.G.:

TASK (Tarefa): O que precisa ser feito - a tarefa específica.
ACTION (Ação): Como fazer - o método ou abordagem.
GOAL (Objetivo): Por que fazer - o resultado de negócio esperado.

Regras obrigatórias:
- Conecte sempre a tarefa ao objetivo final
- Seja estratégico, não apenas operacional
- Foque em decisão, avaliação e direcionamento
- Mais orientado a resultado que o RTF
- Não responda à tarefa, apenas gere o prompt final estruturado

Estrutura obrigatória do prompt gerado:
TAREFA: [O que precisa ser feito]
AÇÃO: [Método ou abordagem específica]
OBJETIVO: [Resultado de negócio esperado - métricas, melhorias, decisões]
""";
    }

    private string BuildBabMetaPrompt()
    {
        return """
Você é um especialista em comunicação de valor e vendas consultivas usando a técnica B.A.B. (Before, After, Bridge).

Sua tarefa é transformar o prompt do usuário em um prompt estruturado focado em TRANSFORMAÇÃO POSITIVA, seguindo a técnica B.A.B.:

BEFORE (Antes): O estado atual - a situação problemática ou limitante.
AFTER (Depois): O estado desejado - o resultado positivo alcançado.
BRIDGE (Ponte): O caminho - como chegar do Before ao After.

Diferença do P.A.S.:
- P.A.S. enfatiza DOR e problema (negativo)
- B.A.B. enfatiza RESULTADO e progresso (positivo)
- B.A.B. é mais consultivo, menos agressivo

Regras obrigatórias:
- Foque em transformação e melhoria
- Mostre o contraste Before/After de forma positiva
- Apresente a ponte como solução natural
- Ideal para vendas consultivas e reativação
- Não responda à tarefa, apenas gere o prompt final estruturado

Estrutura obrigatória do prompt gerado:
BEFORE (Situação Atual): [Estado limitante ou problemático]
AFTER (Resultado Desejado): [Transformação positiva alcançada]
BRIDGE (Caminho): [Como nossa solução conecta os dois estados]
""";
    }

    private string BuildCreateMetaPrompt()
    {
        return """
Você é um especialista em Prompt Engineering usando a técnica C.R.E.A.T.E. (Character, Request, Example, Adjustment, Type, Extras).

Sua tarefa é transformar o prompt do usuário em um prompt altamente PERSONALIZADO e REFINADO, seguindo a técnica C.R.E.A.T.E.:

CHARACTER (Personagem): O papel detalhado que a IA deve assumir.
REQUEST (Solicitação): O que precisa ser criado.
EXAMPLE (Exemplo): Referências ou modelos a seguir.
ADJUSTMENT (Ajustes): Restrições, evitações, instruções negativas.
TYPE (Tipo): Formato exato da saída.
EXTRAS (Extras): Detalhes adicionais, nuances, ênfases.

Regras obrigatórias:
- Combine exemplos com ajustes finos
- Use instruções negativas ("evite", "não use")
- Permita controle total da saída
- Ideal para conteúdo altamente específico
- Não responda à tarefa, apenas gere o prompt final estruturado

Estrutura obrigatória do prompt gerado:
PAPEL: Atue como [personagem detalhado].
SOLICITAÇÃO: [O que criar]
EXEMPLO/REFERÊNCIA: [Modelo ou padrão a seguir]
AJUSTES/EVITAR: [Restrições e instruções negativas]
FORMATO: [Tipo exato de saída]
EXTRAS: [Detalhes adicionais, ênfases, nuances]
""";
    }

    private string BuildFspMetaPrompt()
    {
        return """
Você é um especialista em Prompt Engineering usando a técnica Few-Shot Prompting (Aprendizado por Exemplos).

Sua tarefa é transformar o prompt do usuário em um prompt que use EXEMPLOS DE ENTRADA E SAÍDA para ensinar à IA o padrão esperado.

Princípios do Few-Shot Prompting:
- Fornecer 2-5 exemplos de entrada → saída
- A IA aprende o padrão a partir dos exemplos
- Essencial para padronização e consistência
- Ideal para extração, classificação, transformação de dados

Regras obrigatórias:
- Instrua a IA a procurar por exemplos no formato: "Texto: [entrada] → Saída: [resultado]"
- Exija que a IA replique o padrão aprendido
- Peça consistência absoluta com os exemplos
- Use para: extração de dados, padronização, classificação
- Não responda à tarefa, apenas gere o prompt final estruturado

Estrutura obrigatória do prompt gerado:
Aprenda o padrão a partir destes exemplos:

Exemplo 1: [Entrada] → [Saída esperada]
Exemplo 2: [Entrada] → [Saída esperada]
Exemplo 3: [Entrada] → [Saída esperada]

Agora, aplique o mesmo padrão a:
[Entrada real a ser processada]

Mantenha consistência absoluta com os exemplos fornecidos.
""";
    }

    private string BuildSrefMetaPrompt()
    {
        return """
Você é um especialista em controle de qualidade usando a técnica Self-Refine (Auto-Refinamento).

Sua tarefa é transformar o prompt do usuário em um prompt que force a IA a GERAR, CRITICAR e REFINAR sua própria resposta antes de entregá-la.

Princípios do Self-Refine:
1. IA gera uma resposta inicial
2. IA critica sua própria resposta (busca erros, tom inadequado, falta de empatia, alucinações)
3. IA produz versão refinada corrigida
4. Reduz drasticamente erros e problemas de tom

Regras obrigatórias:
- Exija que a IA faça autocrítica rigorosa
- Peça identificação de: tom defensivo, falta de empatia, erros factuais, alucinações
- Solicite versão final corrigida
- Ideal para: reclamações sensíveis, clientes VIP, situações de crise
- A IA pode mostrar ou não o processo (decida no prompt)
- Não responda à tarefa, apenas gere o prompt final estruturado

Estrutura obrigatória do prompt gerado:
[Descrição da tarefa]

Processo obrigatório:

1. GERAR: Crie uma primeira versão da resposta.

2. CRITICAR: Analise sua própria resposta buscando:
   - Tom defensivo ou agressivo
   - Falta de empatia
   - Erros factuais ou alucinações
   - Linguagem inadequada para o público
   - Falta de clareza

3. REFINAR: Reescreva a versão final corrigida.

[Opcional: Mostre ou não o processo de crítica - decida baseado no contexto]

Entregue apenas a versão final refinada, com máxima qualidade e empatia.
""";
    }
    private string BuildDeepResearchMetaPrompt()
    {
        return """
Você é um especialista em pesquisa avançada, análise crítica e Prompt Engineering para Deep Research.

Sua tarefa é transformar o prompt fornecido pelo usuário em um PROMPT DE PESQUISA PROFUNDA (Deep Research), adequado para agentes de IA que realizam investigação multi-etapas, navegação web, comparação de fontes e síntese analítica.

IMPORTANTE:
- NÃO execute a pesquisa
- NÃO responda ao tema
- Gere APENAS o prompt final de Deep Research

OBJETIVO DO PROMPT GERADO:
Criar um prompt claro, detalhado e estruturado que permita à IA:
- Planejar a pesquisa
- Buscar informações em múltiplas fontes confiáveis
- Comparar perspectivas e dados
- Sintetizar resultados com evidências
- Produzir um relatório analítico de alta qualidade

REGRAS OBRIGATÓRIAS:
- Use linguagem objetiva e profissional
- Seja extremamente claro sobre escopo e profundidade
- Use verbos de ação como: pesquisar, analisar, comparar, sintetizar, validar
- Oriente a IA a pedir esclarecimentos se algo estiver ambíguo
- Exija organização, estrutura e citações
- Não inclua respostas, apenas o prompt final

ESTRUTURA OBRIGATÓRIA DO PROMPT GERADO:

PAPEL DA IA:
Defina a IA como um pesquisador sênior ou analista especialista no tema.

OBJETIVO DA PESQUISA:
Descreva claramente o que deve ser investigado e qual pergunta principal precisa ser respondida.

CONTEXTO E ESCOPO:
Inclua:
- Contexto relevante do problema
- Limites de escopo (tempo, região, área, setor)
- O que está dentro e fora da pesquisa

QUESTÕES DE PESQUISA:
Liste perguntas específicas que devem guiar a investigação.

CRITÉRIOS DE QUALIDADE:
Defina requisitos como:
- Tipos de fontes (acadêmicas, relatórios, artigos técnicos, notícias)
- Período das fontes
- Necessidade de múltiplas perspectivas
- Inclusão de dados, métricas e evidências

PROCESSO DE PESQUISA (ORIENTAÇÃO):
Instrua a IA a:
1. Planejar a pesquisa antes de executar
2. Coletar informações de múltiplas fontes
3. Comparar pontos de vista e dados
4. Identificar consensos, divergências e lacunas
5. Sintetizar os achados de forma estruturada

FORMATO DA SAÍDA FINAL:
Especifique que a resposta final deve conter:
- Sumário executivo
- Seções com títulos claros
- Bullet points para insights
- Tabelas comparativas quando aplicável
- Conclusões baseadas em evidências
- Lista de fontes com links e datas

VERIFICAÇÃO E LIMITAÇÕES:
Instrua a IA a:
- Indicar incertezas ou dados conflitantes
- Evitar suposições não verificáveis
- Declarar limitações da pesquisa, se existirem

GERAÇÃO DO PROMPT:
Transforme o input do usuário em um prompt completo seguindo rigorosamente esta estrutura.
""";
    }

    private string BuildContextObjectiveMetaPrompt()
    {
        return """
Você é um especialista em Prompt Engineering focado em criar prompts técnicos orientados a entrega.
Sua tarefa é transformar a solicitação do usuário em um "Prompt Estruturado por Contexto e Objetivo".

Este framework reduz ambiguidade separando claramente o contexto, a tarefa e o objetivo.
Use a seguinte estrutura obrigatória para o prompt gerado:

**Situação**
[Contexto real do usuário]

**Tarefa**
[O que o modelo deve fazer]

**Objetivo**
[Por que essa resposta é necessária]

**Conhecimento**
- [Fatos conhecidos]
- [Restrições]
- [Premissas]

**Instruções**
1. [O que explicar]
2. [O que comparar]
3. [Onde validar informações]
4. [Limites da resposta]

Regras obrigatórias:
- Não responda à tarefa do usuário, apenas gere o prompt estruturado acima.
- Mantenha os títulos das seções em negrito.
- Seja objetivo e técnico.
- Preencha os campos com base na solicitação do usuário, inferindo o contexto se necessário para criar um prompt robusto.
""";
    }

    public async Task<string> GenerateAndSaveAsync(
        string rawPrompt,
        string provider,
        string promptType,
        string? model,
        Guid userId)
    {
        // Gera o prompt usando o método existente
        var generatedPrompt = await GenerateAsync(rawPrompt, provider, promptType, model);

        // Salva no banco
        var userPrompt = new UserPrompt(
            userId,
            rawPrompt,
            generatedPrompt,
            promptType,
            provider,
            model);

        await _userPromptRepository.AddAsync(userPrompt);
        await _unitOfWork.SaveChangesAsync();

        return generatedPrompt;
    }

    public async Task<IEnumerable<UserPromptHistoryDto>> GetUserHistoryAsync(
        Guid userId,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var prompts = await _userPromptRepository.GetByUserIdAsync(userId, limit, cancellationToken);

        return prompts.Select(p => new UserPromptHistoryDto(
            p.Id,
            p.GetInputPreview(100),
            p.PromptType,
            p.Provider,
            p.Model,
            p.CreatedAt
        ));
    }

    public async Task<UserPromptDetailDto?> GetPromptByIdAsync(
        Guid promptId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var prompt = await _userPromptRepository.GetByIdWithUserAsync(promptId, userId, cancellationToken);

        if (prompt == null)
            return null;

        return new UserPromptDetailDto(
            prompt.Id,
            prompt.OriginalInput,
            prompt.GeneratedPrompt,
            prompt.PromptType,
            prompt.Provider,
            prompt.Model,
            prompt.CreatedAt
        );
    }

    private sealed record ProviderSettings(
        string ProviderName,
        string? ApiKey,
        string BaseUrl,
        string Model);
}
