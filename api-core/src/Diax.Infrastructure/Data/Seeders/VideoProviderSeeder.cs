using Diax.Domain.AI;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Seeders;

/// <summary>
/// Seeds all free-tier video generation providers and their models into the database.
/// Integrates: FAL.ai, Pika, Kling, Runway, HuggingFace, Replicate, Shotstack
/// </summary>
public static class VideoProviderSeeder
{
    private const string BrandColorPrimary = "#6366F1";
    private const string BrandColorSecondary = "#8B5CF6";

    public static async Task SeedVideoProvidersAsync(DiaxDbContext db)
    {
        // FAL.ai — Primary aggregator (Kling, Luma, Minimax, CogVideoX, HunYuan, Mochi, LTX-Video)
        await SeedFalAiAsync(db);

        // Individual providers (some accessed via FAL, some have direct APIs)
        await SeedPikaAsync(db);
        await SeedKlingAsync(db);
        await SeedRunwayAsync(db);
        await SeedHuggingFaceAsync(db);
        await SeedReplicateAsync(db);
        await SeedShotstackAsync(db);

        await db.SaveChangesAsync();
    }

    private static async Task SeedFalAiAsync(DiaxDbContext db)
    {
        const string providerKey = "fal-ai";

        // Check if already exists
        if (await db.AiProviders.AnyAsync(p => p.Key == providerKey))
            return;

        var provider = new AiProvider(
            key: providerKey,
            name: "FAL.ai",
            supportsListModels: true,
            baseUrl: "https://api.fal.ai");

        provider.Enable();
        db.AiProviders.Add(provider);
        await db.SaveChangesAsync();

        // Add quota: unlimited for aggregator, tracked per sub-provider
        var quota = new AiProviderQuota(
            aiProviderId: provider.Id,
            quotaType: "Credits",
            resetFrequency: "Daily",
            dailyCreditsLimit: null, // No hard limit on aggregator
            isEnforced: false);

        db.AiProviderQuotas.Add(quota);

        // Add models for Kling (most popular via FAL)
        var klingModels = new[]
        {
            new AiModel(
                providerId: provider.Id,
                modelKey: "fal-ai/kling-video/v1/standard/txt2video",
                displayName: "Kling 3.0 (Text-to-Video)",
                isDiscovered: false),
            new AiModel(
                providerId: provider.Id,
                modelKey: "fal-ai/kling-video/v1/standard/image-to-video",
                displayName: "Kling 3.0 (Image-to-Video)",
                isDiscovered: false),
            new AiModel(
                providerId: provider.Id,
                modelKey: "fal-ai/kling-video/v1.6/pro/text-to-video",
                displayName: "Kling 1.6 (Pro - Text-to-Video)",
                isDiscovered: false),
        };

        foreach (var model in klingModels)
            db.AiModels.Add(model);

        // Add Luma models
        var lumaModels = new[]
        {
            new AiModel(
                providerId: provider.Id,
                modelKey: "fal-ai/luma-photon",
                displayName: "Luma Photon (Text-to-Video)",
                isDiscovered: false),
            new AiModel(
                providerId: provider.Id,
                modelKey: "fal-ai/luma-dream-machine",
                displayName: "Luma Dream Machine",
                isDiscovered: false),
        };

        foreach (var model in lumaModels)
            db.AiModels.Add(model);

        // Add other FAL-hosted models
        var otherFalModels = new[]
        {
            new AiModel(
                providerId: provider.Id,
                modelKey: "fal-ai/minimax-video-01",
                displayName: "Minimax Video 01",
                isDiscovered: false),
            new AiModel(
                providerId: provider.Id,
                modelKey: "fal-ai/cogvideox-5b",
                displayName: "CogVideoX-5B",
                isDiscovered: false),
            new AiModel(
                providerId: provider.Id,
                modelKey: "fal-ai/hunyuan-video",
                displayName: "HunYuan Video",
                isDiscovered: false),
            new AiModel(
                providerId: provider.Id,
                modelKey: "fal-ai/mochi-1",
                displayName: "Mochi 1 (Genmo)",
                isDiscovered: false),
            new AiModel(
                providerId: provider.Id,
                modelKey: "fal-ai/ltx-video",
                displayName: "LTX-Video",
                isDiscovered: false),
        };

        foreach (var model in otherFalModels)
            db.AiModels.Add(model);
    }

    private static async Task SeedPikaAsync(DiaxDbContext db)
    {
        const string providerKey = "pika";

        if (await db.AiProviders.AnyAsync(p => p.Key == providerKey))
            return;

        var provider = new AiProvider(
            key: providerKey,
            name: "Pika",
            supportsListModels: false,
            baseUrl: "https://api.pika.art"); // Note: accessed via FAL.ai in practice

        provider.Enable();
        db.AiProviders.Add(provider);
        await db.SaveChangesAsync();

        // Quota: 66 credits/day (free tier)
        var quota = new AiProviderQuota(
            aiProviderId: provider.Id,
            quotaType: "Credits",
            resetFrequency: "Daily",
            dailyCreditsLimit: 66,
            isEnforced: true);

        db.AiProviderQuotas.Add(quota);

        var model = new AiModel(
            providerId: provider.Id,
            modelKey: "pika-v1",
            displayName: "Pika 1.0 (via FAL)",
            isDiscovered: false);

        db.AiModels.Add(model);
    }

    private static async Task SeedKlingAsync(DiaxDbContext db)
    {
        const string providerKey = "kling";

        if (await db.AiProviders.AnyAsync(p => p.Key == providerKey))
            return;

        var provider = new AiProvider(
            key: providerKey,
            name: "Kling",
            supportsListModels: false,
            baseUrl: "https://api.kling.kuaishou.com");

        provider.Enable();
        db.AiProviders.Add(provider);
        await db.SaveChangesAsync();

        // Quota: 66 credits/day (free tier)
        var quota = new AiProviderQuota(
            aiProviderId: provider.Id,
            quotaType: "Credits",
            resetFrequency: "Daily",
            dailyCreditsLimit: 66,
            isEnforced: true);

        db.AiProviderQuotas.Add(quota);

        var models = new[]
        {
            new AiModel(
                providerId: provider.Id,
                modelKey: "kling-3.0",
                displayName: "Kling 3.0",
                isDiscovered: false),
            new AiModel(
                providerId: provider.Id,
                modelKey: "kling-1.6",
                displayName: "Kling 1.6",
                isDiscovered: false),
        };

        foreach (var model in models)
            db.AiModels.Add(model);
    }

    private static async Task SeedRunwayAsync(DiaxDbContext db)
    {
        const string providerKey = "runway";

        if (await db.AiProviders.AnyAsync(p => p.Key == providerKey))
            return;

        var provider = new AiProvider(
            key: providerKey,
            name: "Runway",
            supportsListModels: false,
            baseUrl: "https://api.runwayml.com/v1");

        provider.Enable();
        db.AiProviders.Add(provider);
        await db.SaveChangesAsync();

        // Quota: 3 generations/day (free tier limit)
        var quota = new AiProviderQuota(
            aiProviderId: provider.Id,
            quotaType: "Generations",
            resetFrequency: "Daily",
            dailyGenerationLimit: 3,
            isEnforced: true);

        db.AiProviderQuotas.Add(quota);

        var models = new[]
        {
            new AiModel(
                providerId: provider.Id,
                modelKey: "gen4",
                displayName: "Runway Gen-4",
                isDiscovered: false),
            new AiModel(
                providerId: provider.Id,
                modelKey: "gen4-images",
                displayName: "Runway Gen-4 Images",
                isDiscovered: false),
        };

        foreach (var model in models)
            db.AiModels.Add(model);
    }

    private static async Task SeedHuggingFaceAsync(DiaxDbContext db)
    {
        const string providerKey = "huggingface";

        if (await db.AiProviders.AnyAsync(p => p.Key == providerKey))
            return;

        var provider = new AiProvider(
            key: providerKey,
            name: "HuggingFace",
            supportsListModels: true,
            baseUrl: "https://api-inference.huggingface.co");

        provider.Enable();
        db.AiProviders.Add(provider);
        await db.SaveChangesAsync();

        // Quota: Free tier (limited, enforced by HF)
        var quota = new AiProviderQuota(
            aiProviderId: provider.Id,
            quotaType: "Generations",
            resetFrequency: "Daily",
            dailyGenerationLimit: null, // HF rate limits but no hard daily cap for free
            isEnforced: false);

        db.AiProviderQuotas.Add(quota);

        var models = new[]
        {
            new AiModel(
                providerId: provider.Id,
                modelKey: "stabilityai/stable-video-diffusion-img2vid-xt",
                displayName: "Stable Video Diffusion",
                isDiscovered: false),
            new AiModel(
                providerId: provider.Id,
                modelKey: "THUDM/CogVideoX-5b",
                displayName: "CogVideoX-5B (HuggingFace)",
                isDiscovered: false),
            new AiModel(
                providerId: provider.Id,
                modelKey: "TransformerLabs/LTX-Video",
                displayName: "LTX-Video (HuggingFace)",
                isDiscovered: false),
            new AiModel(
                providerId: provider.Id,
                modelKey: "guoyww/animatediff-motion-lora-base-v1",
                displayName: "AnimateDiff",
                isDiscovered: false),
        };

        foreach (var model in models)
            db.AiModels.Add(model);
    }

    private static async Task SeedReplicateAsync(DiaxDbContext db)
    {
        const string providerKey = "replicate";

        if (await db.AiProviders.AnyAsync(p => p.Key == providerKey))
            return;

        var provider = new AiProvider(
            key: providerKey,
            name: "Replicate",
            supportsListModels: true,
            baseUrl: "https://api.replicate.com/v1");

        provider.Enable();
        db.AiProviders.Add(provider);
        await db.SaveChangesAsync();

        // Quota: Free credits, then pay-per-use
        var quota = new AiProviderQuota(
            aiProviderId: provider.Id,
            quotaType: "Credits",
            resetFrequency: "Daily",
            dailyCreditsLimit: null, // Free tier has credits but no daily reset
            isEnforced: false);

        db.AiProviderQuotas.Add(quota);

        // Add a few popular video models
        var models = new[]
        {
            new AiModel(
                providerId: provider.Id,
                modelKey: "stability-ai/stable-video-diffusion",
                displayName: "Stable Video Diffusion (Replicate)",
                isDiscovered: false),
            new AiModel(
                providerId: provider.Id,
                modelKey: "fofr/animate-anything",
                displayName: "Animate Anything",
                isDiscovered: false),
            new AiModel(
                providerId: provider.Id,
                modelKey: "meta/make-a-video",
                displayName: "Make-A-Video",
                isDiscovered: false),
        };

        foreach (var model in models)
            db.AiModels.Add(model);
    }

    private static async Task SeedShotstackAsync(DiaxDbContext db)
    {
        const string providerKey = "shotstack";

        if (await db.AiProviders.AnyAsync(p => p.Key == providerKey))
            return;

        var provider = new AiProvider(
            key: providerKey,
            name: "Shotstack",
            supportsListModels: false,
            baseUrl: "https://api.shotstack.io/v1");

        provider.Enable();
        db.AiProviders.Add(provider);
        await db.SaveChangesAsync();

        // Quota: Sandbox (free, unlimited) for development
        var quota = new AiProviderQuota(
            aiProviderId: provider.Id,
            quotaType: "Generations",
            resetFrequency: "Daily",
            dailyGenerationLimit: null, // Sandbox unlimited
            isEnforced: false);

        db.AiProviderQuotas.Add(quota);

        var model = new AiModel(
            providerId: provider.Id,
            modelKey: "shotstack-render",
            displayName: "Shotstack Video Composition",
            isDiscovered: false);

        db.AiModels.Add(model);
    }
}
