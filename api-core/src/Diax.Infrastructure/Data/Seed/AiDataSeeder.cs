using Diax.Domain.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Data.Seed;

public static class AiDataSeeder
{
    public static void SeedAiProviders(DiaxDbContext db, ILogger? logger = null)
    {
        logger?.LogInformation("Syncing AI Providers and Models seed data...");

        var seedData = new[]
        {
            new {
                Key = "openai", Name = "OpenAI", ListModels = true, BaseUrl = "https://api.openai.com/v1",
                Models = new[] { ("gpt-4o", "GPT-4o"), ("gpt-4o-mini", "GPT-4o Mini"), ("gpt-3.5-turbo", "GPT-3.5 Turbo") }
            },
            new {
                Key = "gemini", Name = "Google Gemini", ListModels = true, BaseUrl = "https://generativelanguage.googleapis.com",
                Models = new[] {
                    ("gemini-2.5-flash", "Gemini 2.5 Flash"),
                    ("gemini-2.0-flash", "Gemini 2.0 Flash"),
                    ("gemini-flash-latest", "Gemini Flash Latest"),
                    ("gemini-pro-latest", "Gemini Pro Latest"),
                    ("gemma-3-4b-it", "Gemma 3 4B IT"),
                    ("gemma-3-12b-it", "Gemma 3 12B IT"),
                    ("gemini-1.5-flash", "Gemini 1.5 Flash")
                }
            },
            new {
                Key = "perplexity", Name = "Perplexity", ListModels = true, BaseUrl = "https://api.perplexity.ai",
                Models = new[] { ("sonar-pro", "Sonar Pro"), ("sonar", "Sonar") }
            },
            new {
                Key = "deepseek", Name = "DeepSeek", ListModels = true, BaseUrl = "https://api.deepseek.com",
                Models = new[] { ("deepseek-chat", "DeepSeek Chat V3"), ("deepseek-reasoner", "DeepSeek R1 (Reasoner)") }
            },
            new {
                Key = "openrouter", Name = "OpenRouter", ListModels = true, BaseUrl = "https://openrouter.ai/api/v1",
                Models = new[] {
                    ("google/gemini-2.0-flash-exp:free", "Gemini 2.0 Flash Exp (Free)"),
                    ("deepseek/deepseek-r1:free", "DeepSeek R1 (Free)"),
                    ("deepseek/deepseek-r1-distill-llama-70b:free", "DeepSeek R1 Llama 70B (Free)")
                }
            }
        };

        bool anyChanges = false;
        foreach (var s in seedData)
        {
            var provider = db.AiProviders.Include(p => p.Models).FirstOrDefault(p => p.Key == s.Key);
            if (provider == null)
            {
                provider = new AiProvider(s.Key, s.Name, s.ListModels, s.BaseUrl);
                provider.Enable();
                db.AiProviders.Add(provider);
                anyChanges = true;
                logger?.LogInformation("Seeding: Added missing AI Provider: {Provider}", s.Key);
            }

            foreach (var mData in s.Models)
            {
                if (!provider.Models.Any(m => m.ModelKey == mData.Item1))
                {
                    var model = new AiModel(provider.Id, mData.Item1, mData.Item2, false);
                    model.Enable();
                    provider.Models.Add(model);
                    anyChanges = true;
                    logger?.LogInformation("Seeding: Added missing AI Model: {Model} to {Provider}", mData.Item1, s.Key);
                }
            }
        }

        if (anyChanges)
        {
            db.SaveChanges();
            logger?.LogInformation("AI Catalog sync completed.");
        }
    }

    private static AiProvider CreateProvider(string key, string name, bool listModels, string baseUrl, (string key, string name)[] models)
    {
        // Keeping this for compatibility or if needed later, but the logic above replaces its main usage
        var provider = new AiProvider(key, name, listModels, baseUrl);
        foreach (var m in models)
        {
            provider.Models.Add(new AiModel(provider.Id, m.key, m.name, false));
        }
        return provider;
    }
}
