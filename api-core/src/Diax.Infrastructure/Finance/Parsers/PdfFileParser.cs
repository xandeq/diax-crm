using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Diax.Domain.AI;
using Diax.Domain.Finance;
using Diax.Shared.Ai;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;

namespace Diax.Infrastructure.Finance.Parsers;

public class PdfFileParser : IFileParser
{
    private readonly IEnumerable<IAiTextTransformClient> _aiClients;
    private readonly IAiProviderRepository _aiProviderRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PdfFileParser> _logger;

    public string FileType => "PDF";

    public PdfFileParser(
        IEnumerable<IAiTextTransformClient> aiClients,
        IAiProviderRepository aiProviderRepository,
        IConfiguration configuration,
        ILogger<PdfFileParser> logger)
    {
        _aiClients = aiClients;
        _aiProviderRepository = aiProviderRepository;
        _configuration = configuration;
        _logger = logger;
    }

    public async IAsyncEnumerable<ParsedTransaction> ParseAsync(Stream fileStream, [EnumeratorCancellation] CancellationToken ct = default)
    {
        // 1. Extract text from PDF
        var textBuilder = new StringBuilder();
        try
        {
            using (var document = PdfDocument.Open(fileStream))
            {
                foreach (var page in document.GetPages())
                {
                    textBuilder.AppendLine(page.Text);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract text from PDF");
            throw new InvalidOperationException("Não foi possível ler o arquivo PDF. Verifique se ele não está corrompido ou protegido por senha.", ex);
        }

        var pdfText = textBuilder.ToString();
        if (string.IsNullOrWhiteSpace(pdfText))
        {
            throw new InvalidOperationException("O PDF parece estar vazio ou não contém texto selecionável (imagem escaneada).");
        }

        if (pdfText.Length > 100000)
        {
             _logger.LogWarning("PDF content truncated. Length: {Length}", pdfText.Length);
             pdfText = pdfText.Substring(0, 100000);
        }

        // 2. Prepare Prompt (Standard for all models)
        var systemPrompt = @"You are a financial data extraction assistant.
You will receive the text content of a bank statement or credit card invoice.
Your task is to extract a list of financial transactions.

Output Format: A JSON object containing a property 'transactions' which is an array of objects.
Each transaction object must have:
- description: string (Merchant name or description)
- amount: number (Negative for expenses/debits, Positive for incomes/credits/payments received)
- date: string (YYYY-MM-DD)

Rules:
1. Ignore page headers, footers, balances, summary lines, and non-transactional text.
2. If it's a credit card statement, individual purchases are expenses (negative amounts).
3. If the amount is ambiguous (e.g. just '10.00'), look for context like 'Debit', 'Credit', '-' sign, or column headers.
4. Convert all dates to ISO 8601 (YYYY-MM-DD). If the year is missing, assume the current year or infer from the statement period if available.
5. Return ONLY valid JSON. Do not wrap in markdown code blocks.

Example Output:
{
  ""transactions"": [
    { ""description"": ""UBER TRIM"", ""amount"": -14.90, ""date"": ""2023-11-15"" },
    { ""description"": ""SALARY DEPOSIT"", ""amount"": 5000.00, ""date"": ""2023-11-05"" }
  ]
}";

        var userMessage = $"Here is the document text:\n\n{pdfText}";

        // 3. Select AI Provider & Options with Fallback Strategy
        // Strategy:
        // 1. Get all enabled providers from DB.
        // 2. Order by preference: DeepSeek > OpenAI > Others.
        // 3. Iterate providers. For each provider, iterate its enabled models (preferring 'smart' ones).
        // 4. Try extract. If success, break. If fail, continue.

        var configurationList = await _aiProviderRepository.GetAllIncludedAsync(ct);
        var enabledProviders = configurationList.Where(p => p.IsEnabled).ToList();

        if (!enabledProviders.Any())
        {
             throw new InvalidOperationException("Nenhum provedor de IA habilitado no sistema.");
        }

        // Sort: DeepSeek first, then OpenAI, then others
        var sortedProviders = enabledProviders
            .OrderByDescending(p => p.Key == "deepseek")
            .ThenByDescending(p => p.Key == "openai")
            .ToList();

        string? content = null;
        Exception? lastException = null;

        foreach (var provider in sortedProviders)
        {
            // Find client implementation
            var client = _aiClients.FirstOrDefault(c => c.ProviderName.Equals(provider.Key, StringComparison.OrdinalIgnoreCase));

            // Manual mapping helpers
            if (client == null)
            {
                if (provider.Key == "openai") client = _aiClients.FirstOrDefault(c => c.ProviderName == "chatgpt");
                if (provider.Key == "openrouter") client = _aiClients.FirstOrDefault(c => c.ProviderName == "openrouter");
            }

            if (client == null)
            {
                _logger.LogWarning("Skipping provider {ProviderName}: Client implementation not found.", provider.Name);
                continue;
            }

            // Get API Key
            // Keys: "ProviderName:ApiKey" or "ProviderKey:ApiKey" or "PromptGenerator:Provider:ApiKey"
            string? apiKey = _configuration[$"{provider.Name}:ApiKey"]
                             ?? _configuration[$"{provider.Key}:ApiKey"]
                             ?? _configuration[$"PromptGenerator:{provider.Name}:ApiKey"]; // Common usage

            // Manual mapping for known keys
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                if (provider.Key == "openai") apiKey = _configuration["OPENAI_API_KEY"];
                if (provider.Key == "deepseek") apiKey = _configuration["DeepSeek:ApiKey"];
                if (provider.Key == "gemini") apiKey = _configuration["GEMINI_API_KEY"];
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                 _logger.LogWarning("Skipping provider {ProviderName}: API Key not configured.", provider.Name);
                 continue;
            }

            // Iterate Models
            var models = provider.Models.Where(m => m.IsEnabled).ToList();
            if (!models.Any())
            {
                // If no models enabled in DB, try a default for the provider
                models.Add(new AiModel(provider.Id, provider.Key == "openai" ? "gpt-4o-mini" : "deepseek-chat", "Default", true));
            }

            // Sort models: Prefer logic/reasoning models (4o, deepseek-chat)
            var sortedModels = models
                .OrderByDescending(m => m.ModelKey.Contains("4o") || m.ModelKey.Contains("deepseek-chat"))
                .ToList();

            foreach (var model in sortedModels)
            {
                try
                {
                    _logger.LogInformation("Attempting PDF extraction with {Provider} / {Model}", provider.Name, model.ModelKey);

                    var options = new AiClientOptions(apiKey, provider.BaseUrl ?? "", model.ModelKey, Temperature: 0);
                    content = await client.TransformAsync(systemPrompt, userMessage, options, ct);

                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        // Success!
                        goto ExtractionSuccess;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed extraction with {Provider} / {Model}. Trying next...", provider.Name, model.ModelKey);
                    lastException = ex;
                }
            }
        }

        ExtractionSuccess:

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("Falha ao analisar o documento com todos os provedores de IA disponíveis.", lastException);
        }

        // Clean up text if it contains markdown code blocks
        content = content.Replace("```json", "").Replace("```", "").Trim();

        // 4. Parse JSON Response
        List<AiExtractedTransaction>? extractedList = null;
        try
        {
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var root = JsonSerializer.Deserialize<AiResponseRoot>(content, jsonOptions);
            extractedList = root?.Transactions;
        }
        catch (JsonException ex)
        {
             _logger.LogError(ex, "Failed to parse AI JSON response: {Content}", content);
             throw new InvalidOperationException("Erro ao processar a resposta da IA. O formato retornado foi inválido.", ex);
        }

        if (extractedList == null)
        {
             yield break;
        }

        // 5. Yield results
        foreach (var item in extractedList)
        {
            if (DateTime.TryParse(item.Date, out var date))
            {
                yield return new ParsedTransaction(item.Description ?? "Sem descrição", item.Amount, date);
            }
        }
    }

    private class AiResponseRoot
    {
        public List<AiExtractedTransaction>? Transactions { get; set; }
    }

    private class AiExtractedTransaction
    {
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public string? Date { get; set; }
    }
}
