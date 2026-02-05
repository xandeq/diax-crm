using Diax.Domain.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Data.Seed;

public static class AiDataSeeder
{
    public static void SeedAiProviders(DiaxDbContext db, ILogger? logger = null)
    {
        if (db.AiProviders.Any())
        {
            return; // Já existem dados
        }

        logger?.LogInformation("Seeding AI Providers and Models...");

        var providers = new List<AiProvider>
        {
            CreateProvider("openai", "OpenAI", true, "https://api.openai.com/v1", new[]
            {
                ("gpt-4o", "GPT-4o"),
                ("gpt-4o-mini", "GPT-4o Mini"),
                ("gpt-3.5-turbo", "GPT-3.5 Turbo")
            }),
            CreateProvider("gemini", "Google Gemini", true, "https://generativelanguage.googleapis.com", new[]
            {
                ("gemini-2.5-flash", "Gemini 2.5 Flash"),
                ("gemini-2.0-flash", "Gemini 2.0 Flash"),
                ("gemini-flash-latest", "Gemini Flash Latest"),
                ("gemini-pro-latest", "Gemini Pro Latest"),
                ("gemma-3-4b-it", "Gemma 3 4B IT"),
                ("gemma-3-12b-it", "Gemma 3 12B IT")
            }),
            CreateProvider("perplexity", "Perplexity", true, "https://api.perplexity.ai", new[]
            {
                ("sonar-pro", "Sonar Pro"),
                ("sonar", "Sonar")
            }),
            CreateProvider("deepseek", "DeepSeek", true, "https://api.deepseek.com", new[]
            {
                ("deepseek-chat", "DeepSeek Chat V3"),
                ("deepseek-reasoner", "DeepSeek R1 (Reasoner)")
            }),
            CreateProvider("openrouter", "OpenRouter", true, "https://openrouter.ai/api/v1", new[]
            {
                ("google/gemini-2.0-flash-exp:free", "Gemini 2.0 Flash Exp (Free)"),
                ("deepseek/deepseek-r1:free", "DeepSeek R1 (Free)"),
                ("deepseek/deepseek-r1-distill-llama-70b:free", "DeepSeek R1 Llama 70B (Free)")
            })
        };

        db.AiProviders.AddRange(providers);
        db.SaveChanges();

        // Habilitar todos por padrão neste seed inicial para facilitar o desenvolvimento
        foreach(var p in providers)
        {
            p.Enable();
            foreach(var m in p.Models)
            {
                m.Enable();
            }
        }
        db.SaveChanges();

        logger?.LogInformation("AI Providers seeded successfully.");
    }

    private static AiProvider CreateProvider(string key, string name, bool listModels, string baseUrl, (string key, string name)[] models)
    {
        var provider = new AiProvider(key, name, listModels, baseUrl);

        foreach (var m in models)
        {
            provider.Models.Add(new AiModel(provider.Id, m.key, m.name, false));
        }

        return provider;
    }
}
