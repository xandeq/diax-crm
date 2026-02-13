using Diax.Domain.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Data.Seed;

/// <summary>
/// AI Data Seeder - Provides minimal fallback for empty databases
/// IMPORTANT: All providers and models should be managed via Admin UI (/admin/ai)
/// This seeder only ensures the database is not completely empty on first run
/// </summary>
public static class AiDataSeeder
{
    public static void SeedAiProviders(DiaxDbContext db, ILogger? logger = null)
    {
        logger?.LogInformation("Checking AI Providers database...");

        // Check if database already has providers
        var existingProviders = db.AiProviders.Any();

        if (existingProviders)
        {
            logger?.LogInformation("AI Providers already configured. Skipping seed (manage via /admin/ai).");
            return;
        }

        // ⚠️ WARNING: Database is empty - adding minimal fallback provider
        logger?.LogWarning("WARNING: No AI providers found in database!");
        logger?.LogWarning("Adding minimal OpenAI fallback. Configure providers via Admin UI: /admin/ai");

        // Minimal fallback: Only OpenAI with 1 model
        var fallbackProvider = new AiProvider(
            key: "openai",
            name: "OpenAI",
            supportsListModels: true,
            baseUrl: "https://api.openai.com/v1"
        );

        // DISABLED by default - admin must:
        // 1. Configure API key via UI
        // 2. Enable provider
        // 3. Discover/add models
        // 4. Assign permissions to groups
        fallbackProvider.Disable();

        var fallbackModel = new AiModel(
            providerId: fallbackProvider.Id,
            modelKey: "gpt-4o-mini",
            displayName: "GPT-4o Mini",
            isMultimodal: false
        );
        fallbackModel.Disable(); // Disabled by default

        fallbackProvider.Models.Add(fallbackModel);

        db.AiProviders.Add(fallbackProvider);
        db.SaveChanges();

        logger?.LogInformation("✅ Minimal fallback provider added (DISABLED)");
        logger?.LogInformation("📋 Next steps:");
        logger?.LogInformation("   1. Navigate to /admin/ai");
        logger?.LogInformation("   2. Create providers (OpenAI, Gemini, DeepSeek, etc.)");
        logger?.LogInformation("   3. Configure API keys for each provider");
        logger?.LogInformation("   4. Discover and enable models");
        logger?.LogInformation("   5. Assign provider/model access to user groups");
    }
}
