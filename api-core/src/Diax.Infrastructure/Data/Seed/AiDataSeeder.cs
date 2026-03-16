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
                // Valid Gemini chat models that support image generation via :generateContent
                // (Gemini doesn't have separate image-gen models; all chat models can generate images)
                new ModelSeed("gemini-2.5-flash",         "Gemini 2.5 Flash"),
                new ModelSeed("gemini-2.0-flash",         "Gemini 2.0 Flash"),
                new ModelSeed("gemini-2.5-pro",           "Gemini 2.5 Pro"),
                new ModelSeed("gemini-2.0-pro",           "Gemini 2.0 Pro"),
                new ModelSeed("gemini-1.5-pro",           "Gemini 1.5 Pro"),
                new ModelSeed("gemini-1.5-flash",         "Gemini 1.5 Flash"),
                new ModelSeed("gemini-3-flash-preview",   "Gemini 3 Flash Preview"),
                new ModelSeed("gemini-3-pro-preview",     "Gemini 3 Pro Preview"),
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

                // --- Text generation (Serverless Inference API) ---
                new ModelSeed("meta-llama/Meta-Llama-3.1-8B-Instruct",  "LLaMA 3.1 8B Instruct (gratuito)"),
                new ModelSeed("meta-llama/Meta-Llama-3.1-70B-Instruct", "LLaMA 3.1 70B Instruct"),
                new ModelSeed("Qwen/Qwen2.5-72B-Instruct",              "Qwen 2.5 72B Instruct"),
                new ModelSeed("mistralai/Mistral-7B-Instruct-v0.3",     "Mistral 7B Instruct v0.3 (gratuito)"),
                new ModelSeed("microsoft/Phi-3.5-mini-instruct",        "Phi 3.5 Mini Instruct (gratuito)"),
                new ModelSeed("google/gemma-2-9b-it",                   "Gemma 2 9B Instruct (gratuito)"),

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
            Key: "groq",
            Name: "Groq",
            BaseUrl: "https://api.groq.com/openai/v1",
            SupportsListModels: true,
            Models: new()
            {
                new ModelSeed("llama-3.3-70b-versatile",           "LLaMA 3.3 70B Versatile"),
                new ModelSeed("llama-3.1-8b-instant",              "LLaMA 3.1 8B Instant"),
                new ModelSeed("llama3-70b-8192",                   "LLaMA 3 70B"),
                new ModelSeed("llama3-8b-8192",                    "LLaMA 3 8B"),
                new ModelSeed("mixtral-8x7b-32768",                "Mixtral 8x7B"),
                new ModelSeed("gemma2-9b-it",                      "Gemma 2 9B"),
                new ModelSeed("gemma-7b-it",                       "Gemma 7B"),
            }),

        new ProviderSeed(
            Key: "cerebras",
            Name: "Cerebras",
            BaseUrl: "https://api.cerebras.ai/v1",
            SupportsListModels: false,
            Models: new()
            {
                new ModelSeed("llama-3.3-70b",   "LLaMA 3.3 70B (Cerebras)"),
                new ModelSeed("llama3.1-70b",    "LLaMA 3.1 70B (Cerebras)"),
                new ModelSeed("llama3.1-8b",     "LLaMA 3.1 8B (Cerebras)"),
                new ModelSeed("llama-4-scout-17b-16e-instruct", "LLaMA 4 Scout 17B (Cerebras)"),
            }),

        new ProviderSeed(
            Key: "openrouter",
            Name: "OpenRouter",
            BaseUrl: "https://openrouter.ai/api/v1",
            SupportsListModels: true,
            Models: new()
            {
                // --- Free image models ---
                new ModelSeed("black-forest-labs/flux-schnell",                       "FLUX Schnell (gratuito)"),
                new ModelSeed("black-forest-labs/flux-1-schnell:free",                "FLUX 1 Schnell Free"),
                new ModelSeed("black-forest-labs/flux-1-dev:free",                    "FLUX 1 Dev (gratuito)"),
                new ModelSeed("stabilityai/stable-diffusion-xl-base-1.0:free",        "SDXL Base (gratuito)"),

                // --- Paid image models ---
                new ModelSeed("black-forest-labs/flux-1.1-pro",                       "FLUX 1.1 Pro"),
                new ModelSeed("black-forest-labs/flux-dev",                           "FLUX Dev"),
                new ModelSeed("black-forest-labs/flux-kontext-pro",                   "FLUX Kontext Pro"),
                new ModelSeed("stability-ai/stable-diffusion-3.5-large",              "SD 3.5 Large"),
                new ModelSeed("google/gemini-2.5-flash-image",                        "Nano Banana (Gemini 2.5 Flash)"),
                new ModelSeed("google/gemini-3.1-flash-image-preview",                "Nano Banana 2 (Gemini 3.1 Flash)"),
                new ModelSeed("google/gemini-3-pro-image-preview",                    "Nano Banana Pro (Gemini 3 Pro)"),
                new ModelSeed("google/imagen-3",                                      "Google Imagen 3"),
                new ModelSeed("openai/gpt-image-1",                                   "GPT Image 1 (via OpenRouter)"),
            }),

        new ProviderSeed(
            Key: "replicate",
            Name: "Replicate (Gratuito)",
            BaseUrl: "https://api.replicate.com/v1",
            SupportsListModels: false,
            Models: new()
            {
                // Image generation (free tier available)
                new ModelSeed("black-forest-labs/flux-schnell",                    "FLUX Schnell (gratuito)"),
                new ModelSeed("black-forest-labs/flux-pro",                        "FLUX Pro"),
                new ModelSeed("stability-ai/stable-diffusion-3.5-large",           "Stable Diffusion 3.5 Large"),
                new ModelSeed("stability-ai/stable-diffusion-3-medium",            "Stable Diffusion 3 Medium (gratuito)"),
                new ModelSeed("stability-ai/stable-diffusion-xl",                  "SDXL (gratuito)"),
                new ModelSeed("openai/dall-e-3",                                   "DALL-E 3 (via Replicate)"),
                // Text-to-video (free tier available)
                new ModelSeed("cjwbw/frame-interpolation",                         "Frame Interpolation"),
                new ModelSeed("deforum-art/deforum-stable-diffusion",              "Deforum Stable Diffusion"),
            }),

        new ProviderSeed(
            Key: "togetherai",
            Name: "Together AI (Gratuito)",
            BaseUrl: "https://api.together.xyz/v1",
            SupportsListModels: false,
            Models: new()
            {
                // Open-source models (free tier: 1M tokens/month)
                new ModelSeed("meta-llama/Llama-3.3-70B-Instruct-Turbo",           "LLaMA 3.3 70B (gratuito)"),
                new ModelSeed("meta-llama/Llama-3.1-70B-Instruct-Turbo",           "LLaMA 3.1 70B (gratuito)"),
                new ModelSeed("meta-llama/Llama-3-70B-Chat-Hf",                    "LLaMA 3 70B Chat (gratuito)"),
                new ModelSeed("meta-llama/Llama-3-8B-Instruct-Turbo",              "LLaMA 3 8B (gratuito)"),
                new ModelSeed("mistralai/Mixtral-8x7B-Instruct-v0.1",              "Mixtral 8x7B (gratuito)"),
                new ModelSeed("mistralai/Mistral-7B-Instruct-v0.3",                "Mistral 7B (gratuito)"),
                new ModelSeed("qwen/Qwen2.5-72B-Instruct-Turbo",                   "Qwen 2.5 72B (gratuito)"),
                new ModelSeed("NousResearch/Nous-Hermes-2-Mixtral-8x7B-DPO",      "Nous Hermes 2 (gratuito)"),
                // Image generation (free tier available)
                new ModelSeed("black-forest-labs/flux-1-schnell-fp8",              "FLUX Schnell (gratuito)"),
                new ModelSeed("black-forest-labs/flux-1-dev-fp8",                  "FLUX Dev (gratuito)"),
                new ModelSeed("stabilityai/stable-diffusion-xl-base-1.0",          "SDXL Base (gratuito)"),
            }),

        new ProviderSeed(
            Key: "ollama",
            Name: "Ollama (Local - Gratuito)",
            BaseUrl: "http://localhost:11434/api",
            SupportsListModels: false,
            Models: new()
            {
                // Popular open-source models for local deployment
                // Note: Requires Ollama to be installed and running locally
                new ModelSeed("llama2",                                             "LLaMA 2 (local)"),
                new ModelSeed("llama2-uncensored",                                  "LLaMA 2 Uncensored (local)"),
                new ModelSeed("mistral",                                            "Mistral (local)"),
                new ModelSeed("neural-chat",                                        "Neural Chat (local)"),
                new ModelSeed("dolphin-mixtral",                                    "Dolphin Mixtral (local)"),
                new ModelSeed("stable-diffusion",                                   "Stable Diffusion (local image gen)"),
            }),
    };

    public static void SeedAiProviders(DiaxDbContext db, ILogger? logger = null)
    {
        logger?.LogInformation("[AiDataSeeder] Starting upsert of AI providers and models...");

        // --- Preload everything in bulk to minimize round-trips ---
        var adminGroup = db.UserGroups.FirstOrDefault(g => g.Key == "system-admin");
        if (adminGroup == null)
            logger?.LogWarning("[AiDataSeeder] system-admin group not found — skipping access grants. Run UserSeeder first.");

        var existingProviders = db.AiProviders
            .Select(p => new { p.Id, p.Key })
            .ToList();

        var existingModelKeysByProvider = db.AiModels
            .GroupBy(m => m.ProviderId)
            .ToDictionary(g => g.Key, g => g.Select(m => m.ModelKey).ToHashSet());

        var existingProviderAccesses = adminGroup != null
            ? db.GroupAiProviderAccesses.Where(a => a.GroupId == adminGroup.Id).Select(a => a.ProviderId).ToHashSet()
            : new HashSet<Guid>();

        var existingModelAccesses = adminGroup != null
            ? db.GroupAiModelAccesses.Where(a => a.GroupId == adminGroup.Id).Select(a => a.AiModelId).ToHashSet()
            : new HashSet<Guid>();

        int addedProviders = 0, addedModels = 0, grantedProviderAccess = 0, grantedModelAccess = 0;

        foreach (var seed in KnownProviders)
        {
            // --- Upsert provider ---
            var existingProvider = existingProviders.FirstOrDefault(p => p.Key == seed.Key);
            Guid providerId;

            if (existingProvider == null)
            {
                var provider = new AiProvider(seed.Key, seed.Name, seed.SupportsListModels, seed.BaseUrl);
                provider.Enable();
                db.AiProviders.Add(provider);
                db.SaveChanges(); // flush once to get real ID
                providerId = provider.Id;
                existingProviders.Add(new { provider.Id, provider.Key });
                addedProviders++;
                logger?.LogInformation("[AiDataSeeder] ✅ Added provider: {Key}", seed.Key);
            }
            else
            {
                providerId = existingProvider.Id;
            }

            // --- Grant provider access ---
            if (adminGroup != null && !existingProviderAccesses.Contains(providerId))
            {
                db.GroupAiProviderAccesses.Add(new GroupAiProviderAccess(adminGroup.Id, providerId));
                existingProviderAccesses.Add(providerId);
                grantedProviderAccess++;
            }

            // --- Batch-add all new models (no SaveChanges per model) ---
            var knownKeys = existingModelKeysByProvider.GetValueOrDefault(providerId) ?? new HashSet<string>();
            var newModels = new List<AiModel>();

            foreach (var modelSeed in seed.Models)
            {
                if (!knownKeys.Contains(modelSeed.Key))
                {
                    var model = new AiModel(providerId, modelSeed.Key, modelSeed.DisplayName, isDiscovered: false);
                    model.Enable();
                    db.AiModels.Add(model);
                    newModels.Add(model);
                    knownKeys.Add(modelSeed.Key);
                    addedModels++;
                }
            }

            // Flush all new models + provider access grant in one shot
            db.SaveChanges();

            // --- Grant model access for newly added models (IDs now available) ---
            if (adminGroup != null && newModels.Count > 0)
            {
                foreach (var model in newModels)
                {
                    if (!existingModelAccesses.Contains(model.Id))
                    {
                        db.GroupAiModelAccesses.Add(new GroupAiModelAccess(adminGroup.Id, model.Id));
                        existingModelAccesses.Add(model.Id);
                        grantedModelAccess++;
                    }
                }
                db.SaveChanges(); // flush model access grants
            }
        }

        logger?.LogInformation(
            "[AiDataSeeder] Done. Added {Providers} providers, {Models} models. " +
            "Granted {PA} provider accesses, {MA} model accesses to admin group.",
            addedProviders, addedModels, grantedProviderAccess, grantedModelAccess);
    }

    private record ProviderSeed(string Key, string Name, string? BaseUrl, bool SupportsListModels, List<ModelSeed> Models);
    private record ModelSeed(string Key, string DisplayName);
}
