using Diax.Domain.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Data.Seed;

/// <summary>
/// AI Data Seeder — upsert approach.
/// Runs on every startup to add/enable any new providers or models not yet in the DB.
/// Never disables or removes existing entries — only adds new ones.
/// Grants the system-admin group access to all providers and models.
/// </summary>
public static class AiDataSeeder
{
    // ---------------------------------------------------------
    // Provider + model catalog. Add new entries here freely.
    // ---------------------------------------------------------
    private static readonly List<ProviderSeed> KnownProviders = new()
    {
        new ProviderSeed(
            Key: "openai",
            Name: "OpenAI",
            BaseUrl: "https://api.openai.com/v1",
            SupportsListModels: true,
            Models: new()
            {
                new ModelSeed("dall-e-3",    "DALL·E 3"),
                new ModelSeed("dall-e-2",    "DALL·E 2"),
                new ModelSeed("gpt-image-1", "GPT Image 1"),
            }),

        new ProviderSeed(
            Key: "gemini",
            Name: "Google Gemini",
            BaseUrl: "https://generativelanguage.googleapis.com",
            SupportsListModels: false,
            Models: new()
            {
                new ModelSeed("imagen-3.0-generate-002",               "Imagen 3"),
                new ModelSeed("imagen-3.0-fast-generate-001",          "Imagen 3 Fast"),
                new ModelSeed("gemini-2.0-flash-exp-image-generation", "Gemini 2.0 Flash Image"),
                new ModelSeed("gemini-2.5-flash-preview-04-17",        "Gemini 2.5 Flash (Image)"),
            }),

        new ProviderSeed(
            Key: "falai",
            Name: "FAL.ai",
            BaseUrl: "https://fal.run",
            SupportsListModels: false,
            Models: new()
            {
                // --- Image models ---
                new ModelSeed("fal-ai/flux/dev",                     "FLUX Dev"),
                new ModelSeed("fal-ai/flux/schnell",                 "FLUX Schnell (rápido)"),
                new ModelSeed("fal-ai/flux-pro",                     "FLUX Pro"),
                new ModelSeed("fal-ai/flux-pro/v1.1",                "FLUX Pro v1.1"),
                new ModelSeed("fal-ai/flux-realism",                 "FLUX Realism"),
                new ModelSeed("fal-ai/fast-sdxl",                    "Fast SDXL"),
                new ModelSeed("fal-ai/stable-diffusion-v35-large",   "Stable Diffusion 3.5 Large"),
                new ModelSeed("fal-ai/ideogram/v2",                  "Ideogram v2"),
                new ModelSeed("fal-ai/flux/dev/image-to-image",      "FLUX Dev Img2Img"),
                new ModelSeed("fal-ai/flux-pro/kontext",             "FLUX Pro Kontext Img2Img"),
                // --- Video models ---
                new ModelSeed("fal-ai/kling-video/v2.1/standard/text-to-video",   "Kling 2.1 Text-to-Video"),
                new ModelSeed("fal-ai/kling-video/v2.1/standard/image-to-video",  "Kling 2.1 Image-to-Video"),
                new ModelSeed("fal-ai/kling-video/v1.6/pro/text-to-video",        "Kling 1.6 Pro Text-to-Video"),
                new ModelSeed("fal-ai/kling-video/v1.6/pro/image-to-video",       "Kling 1.6 Pro Img-to-Video"),
                new ModelSeed("fal-ai/wan/v1.3/text-to-video",                    "WAN v1.3 Text-to-Video"),
                new ModelSeed("fal-ai/wan/v1.3/image-to-video",                   "WAN v1.3 Image-to-Video"),
                new ModelSeed("fal-ai/wan-i2v",                                   "WAN Image-to-Video"),
                new ModelSeed("fal-ai/wan-t2v",                                   "WAN Text-to-Video"),
                new ModelSeed("fal-ai/luma-dream-machine",                        "Luma Dream Machine"),
                new ModelSeed("fal-ai/luma-dream-machine/image-to-video",         "Luma Dream Machine Img2Video"),
                new ModelSeed("fal-ai/minimax/video-01",                          "Minimax Video-01"),
                new ModelSeed("fal-ai/minimax/video-01-live",                     "Minimax Video-01 Live"),
                new ModelSeed("fal-ai/cogvideox-5b",                              "CogVideoX 5B"),
                new ModelSeed("fal-ai/hunyuan-video",                             "HunyuanVideo"),
                new ModelSeed("fal-ai/hunyuan-video-image-to-video",              "HunyuanVideo Img2Video"),
                new ModelSeed("fal-ai/mochi-v1",                                  "Mochi v1"),
                new ModelSeed("fal-ai/ltx-video",                                 "LTX-Video"),
                new ModelSeed("fal-ai/ltx-video/image-to-video",                  "LTX-Video Img2Video"),
            }),

        new ProviderSeed(
            Key: "grok",
            Name: "xAI Grok",
            BaseUrl: "https://api.x.ai/v1",
            SupportsListModels: false,
            Models: new()
            {
                new ModelSeed("grok-2-image-1212", "Grok 2 Aurora (gratuito)"),
                new ModelSeed("grok-2-aurora",     "Grok Aurora"),
            }),

        new ProviderSeed(
            Key: "huggingface",
            Name: "HuggingFace",
            BaseUrl: "https://api-inference.huggingface.co",
            SupportsListModels: false,
            Models: new()
            {
                // FLUX family (best open-source)
                new ModelSeed("black-forest-labs/FLUX.1-schnell",        "FLUX.1 Schnell (gratuito, rápido)"),
                new ModelSeed("black-forest-labs/FLUX.1-dev",            "FLUX.1 Dev"),
                new ModelSeed("black-forest-labs/FLUX.1-kontext-dev",    "FLUX.1 Kontext Dev"),
                new ModelSeed("black-forest-labs/FLUX.2-dev",            "FLUX.2 Dev"),
                new ModelSeed("black-forest-labs/FLUX.2-max",            "FLUX.2 Max"),
                new ModelSeed("black-forest-labs/FLUX.2-flex",           "FLUX.2 Flex"),

                // Stable Diffusion family
                new ModelSeed("stabilityai/stable-diffusion-xl-base-1.0",        "SDXL Base 1.0 (gratuito)"),
                new ModelSeed("stabilityai/stable-diffusion-xl-refiner-1.0",     "SDXL Refiner"),
                new ModelSeed("runwayml/stable-diffusion-v1-5",                  "Stable Diffusion v1.5 (gratuito)"),
                new ModelSeed("stabilityai/stable-diffusion-3-medium-diffusers", "SD3 Medium (gratuito)"),
                new ModelSeed("stabilityai/stable-diffusion-3.5-large",          "SD 3.5 Large"),
                new ModelSeed("stabilityai/stable-diffusion-3.5-medium",         "SD 3.5 Medium"),
                new ModelSeed("stabilityai/stable-diffusion-3.5-large-turbo",    "SD 3.5 Large Turbo"),

                // ByteDance Hyper-SD (very fast)
                new ModelSeed("ByteDance/Hyper-SD",    "Hyper-SD (gratuito, muito rápido)"),
                new ModelSeed("ByteDance/Hyper-SDXL",  "Hyper-SDXL (gratuito, rápido)"),

                // DeepFloyd IF (great for text in images)
                new ModelSeed("deepfloyd/IF-I-XL-v1.0", "DeepFloyd IF XL"),
                new ModelSeed("deepfloyd/IF",            "DeepFloyd IF (texto em imagens)"),

                // Qwen Image (Alibaba)
                new ModelSeed("Qwen/Qwen-Image",      "Qwen Image"),
                new ModelSeed("Qwen/Qwen-Image-2512", "Qwen Image 2512"),
                new ModelSeed("Qwen/Qwen-Image-Edit", "Qwen Image Edit"),

                // Z-Image (Tongyi-MAI / Alibaba)
                new ModelSeed("Tongyi-MAI/Z-Image",       "Z-Image"),
                new ModelSeed("Tongyi-MAI/Z-Image-Turbo", "Z-Image Turbo"),
                new ModelSeed("Tongyi-MAI/Z-Image-Edit",  "Z-Image Edit"),

                // GLM Image (ZhipuAI)
                new ModelSeed("zai-org/glm-image", "GLM Image (ZhipuAI)"),
                new ModelSeed("THUDM/glm-image",   "GLM Image (THUDM)"),

                // Tencent Hunyuan Image
                new ModelSeed("tencent/Hunyuan-DiT",    "Hunyuan DiT"),
                new ModelSeed("tencent/HunyuanImage-2.1","HunyuanImage 2.1"),
                new ModelSeed("tencent/HunyuanImage-3", "HunyuanImage 3"),
                new ModelSeed("tencent/HunyuanImage",   "HunyuanImage"),

                // HiDream Image
                new ModelSeed("HiDream-ai/HiDream-I1",      "HiDream I1"),
                new ModelSeed("HiDream-ai/HiDream-I1-Fast", "HiDream I1 Fast"),
                new ModelSeed("HiDream-ai/HiDream-I1-Dev",  "HiDream I1 Dev"),

                // Video via HF
                new ModelSeed("stabilityai/stable-video-diffusion-img2vid-xt", "Stable Video Diffusion XT"),
                new ModelSeed("stabilityai/stable-video-diffusion",            "Stable Video Diffusion"),
                new ModelSeed("stabilityai/stable-video-diffusion-img2vid",    "Stable Video Diffusion Img2Vid"),
                new ModelSeed("stabilityai/svd",                               "SVD (Stable Video Diffusion)"),
                new ModelSeed("THUDM/CogVideoX-5B",                            "CogVideoX 5B"),
                new ModelSeed("THUDM/CogVideoX-2B",                            "CogVideoX 2B"),
                new ModelSeed("THUDM/CogVideoX-1.5",                           "CogVideoX 1.5"),
                new ModelSeed("Lightricks/LTX-Video",                          "LTX-Video (Lightricks)"),
                new ModelSeed("Wan-AI/Wan2.1-T2V-14B",                         "WAN 2.1 Text-to-Video 14B"),
                new ModelSeed("Wan-AI/Wan2.1-I2V-14B",                         "WAN 2.1 Image-to-Video 14B"),
                new ModelSeed("Wan-AI/Wan2.1-T2V-1.3B",                        "WAN 2.1 Text-to-Video 1.3B"),
                new ModelSeed("VideoCrafter/VideoCrafter2",                     "VideoCrafter 2"),
                new ModelSeed("VideoCrafter/VideoCrafter1",                     "VideoCrafter 1"),
                new ModelSeed("THUDM/VideoCrafter2",                            "VideoCrafter 2 (THUDM)"),
            }),

        new ProviderSeed(
            Key: "openrouter",
            Name: "OpenRouter",
            BaseUrl: "https://openrouter.ai/api/v1",
            SupportsListModels: true,
            Models: new()
            {
                new ModelSeed("black-forest-labs/flux-1.1-pro",              "FLUX 1.1 Pro"),
                new ModelSeed("black-forest-labs/flux-schnell",              "FLUX Schnell (gratuito)"),
                new ModelSeed("black-forest-labs/flux-dev",                  "FLUX Dev"),
                new ModelSeed("black-forest-labs/flux-kontext-pro",          "FLUX Kontext Pro"),
                new ModelSeed("stability-ai/stable-diffusion-3.5-large",     "SD 3.5 Large"),
                new ModelSeed("google/gemini-2.0-flash-image-generation",    "Gemini 2.0 Flash Image"),
                new ModelSeed("openai/gpt-image-1",                          "GPT Image 1 (via OpenRouter)"),
            }),
    };

    public static void SeedAiProviders(DiaxDbContext db, ILogger? logger = null)
    {
        logger?.LogInformation("[AiDataSeeder] Starting upsert of AI providers and models...");

        // Find admin group to grant access
        var adminGroup = db.UserGroups.FirstOrDefault(g => g.Key == "system-admin");
        if (adminGroup == null)
            logger?.LogWarning("[AiDataSeeder] system-admin group not found — skipping access grants. Run UserSeeder first.");

        int addedProviders = 0, addedModels = 0, grantedProviderAccess = 0, grantedModelAccess = 0;

        foreach (var seed in KnownProviders)
        {
            // Upsert provider
            var provider = db.AiProviders.Include(p => p.Models)
                .FirstOrDefault(p => p.Key == seed.Key);

            if (provider == null)
            {
                provider = new AiProvider(seed.Key, seed.Name, seed.SupportsListModels, seed.BaseUrl);
                provider.Enable();
                db.AiProviders.Add(provider);
                db.SaveChanges(); // flush to get generated ID
                addedProviders++;
                logger?.LogInformation("[AiDataSeeder] ✅ Added provider: {Key}", seed.Key);
            }

            // Grant admin group access to provider
            if (adminGroup != null)
            {
                var hasProviderAccess = db.GroupAiProviderAccesses
                    .Any(a => a.GroupId == adminGroup.Id && a.ProviderId == provider.Id);

                if (!hasProviderAccess)
                {
                    db.GroupAiProviderAccesses.Add(new GroupAiProviderAccess(adminGroup.Id, provider.Id));
                    grantedProviderAccess++;
                }
            }

            // Upsert models
            foreach (var modelSeed in seed.Models)
            {
                var existingModel = db.AiModels
                    .FirstOrDefault(m => m.ProviderId == provider.Id && m.ModelKey == modelSeed.Key);

                if (existingModel == null)
                {
                    var model = new AiModel(provider.Id, modelSeed.Key, modelSeed.DisplayName, isDiscovered: false);
                    model.Enable();
                    db.AiModels.Add(model);
                    db.SaveChanges(); // flush to get generated ID
                    existingModel = model;
                    addedModels++;
                    logger?.LogDebug("[AiDataSeeder] Added model: {Key} ({Provider})", modelSeed.Key, seed.Key);
                }

                // Grant admin group access to model
                if (adminGroup != null)
                {
                    var hasModelAccess = db.GroupAiModelAccesses
                        .Any(a => a.GroupId == adminGroup.Id && a.AiModelId == existingModel.Id);

                    if (!hasModelAccess)
                    {
                        db.GroupAiModelAccesses.Add(new GroupAiModelAccess(adminGroup.Id, existingModel.Id));
                        grantedModelAccess++;
                    }
                }
            }

            db.SaveChanges();
        }

        logger?.LogInformation(
            "[AiDataSeeder] Done. Added {Providers} providers, {Models} models. " +
            "Granted {PA} provider accesses, {MA} model accesses to admin group.",
            addedProviders, addedModels, grantedProviderAccess, grantedModelAccess);
    }

    private record ProviderSeed(string Key, string Name, string? BaseUrl, bool SupportsListModels, List<ModelSeed> Models);
    private record ModelSeed(string Key, string DisplayName);
}
