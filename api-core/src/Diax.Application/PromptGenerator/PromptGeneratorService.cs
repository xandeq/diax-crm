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
    private const string DefaultPromptType = "professional";
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

    public async Task<string> GenerateAsync(string rawPrompt, string provider, string promptType)
    {
        if (string.IsNullOrWhiteSpace(rawPrompt))
        {
            throw new ArgumentException("Prompt não pode ser vazio.", nameof(rawPrompt));
        }

        var normalizedProvider = NormalizeProvider(provider);
        var normalizedPromptType = NormalizePromptType(promptType);
        var settings = GetProviderSettings(normalizedProvider);

        _logger.LogInformation("Prompt generation started. Provider: {Provider}. PromptType: {PromptType}. RawPromptLength: {Length}",
            normalizedProvider, normalizedPromptType, rawPrompt.Length);

        var metaPrompt = BuildMetaPrompt(normalizedPromptType);
        var finalPrompt = await SendPromptAsync(settings, metaPrompt, rawPrompt);

        _logger.LogInformation("Prompt generation completed. Provider: {Provider}. PromptType: {PromptType}.",
            normalizedProvider, normalizedPromptType);

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

    private string NormalizePromptType(string promptType)
    {
        if (string.IsNullOrWhiteSpace(promptType))
        {
            return DefaultPromptType;
        }

        return promptType.Trim().ToLowerInvariant();
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

    private sealed record ProviderSettings(
        string ProviderName,
        string? ApiKey,
        string BaseUrl,
        string Model);
}
