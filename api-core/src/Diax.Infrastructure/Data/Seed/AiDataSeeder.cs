using Diax.Domain.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

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
                new ModelSeed("dall-e-3",    "DALL·E 3", SupportsImage: true),
                new ModelSeed("dall-e-2",    "DALL·E 2", SupportsImage: true),
                new ModelSeed("gpt-image-1", "GPT Image 1", SupportsImage: true),
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
                new ModelSeed("gemini-2.5-flash",         "Gemini 2.5 Flash", SupportsImage: true),
                new ModelSeed("gemini-2.0-flash",         "Gemini 2.0 Flash", SupportsImage: true),
                new ModelSeed("gemini-2.5-pro",           "Gemini 2.5 Pro", SupportsImage: true),
                new ModelSeed("gemini-2.0-pro",           "Gemini 2.0 Pro", SupportsImage: true),
                new ModelSeed("gemini-1.5-pro",           "Gemini 1.5 Pro", SupportsImage: true),
                new ModelSeed("gemini-1.5-flash",         "Gemini 1.5 Flash", SupportsImage: true),
                new ModelSeed("gemini-3-flash-preview",   "Gemini 3 Flash Preview", SupportsImage: true),
                new ModelSeed("gemini-3-pro-preview",     "Gemini 3 Pro Preview", SupportsImage: true),
            }),

        new ProviderSeed(
            Key: "falai",
            Name: "FAL.ai",
            BaseUrl: "https://fal.run",
            SupportsListModels: false,
            Models: new()
            {
                // --- Image models ---
                new ModelSeed("fal-ai/flux/dev",                     "FLUX Dev", SupportsImage: true),
                new ModelSeed("fal-ai/flux/schnell",                 "FLUX Schnell (rápido)", SupportsImage: true),
                new ModelSeed("fal-ai/flux-pro",                     "FLUX Pro", SupportsImage: true),
                new ModelSeed("fal-ai/flux-pro/v1.1",                "FLUX Pro v1.1", SupportsImage: true),
                new ModelSeed("fal-ai/flux-realism",                 "FLUX Realism", SupportsImage: true),
                new ModelSeed("fal-ai/fast-sdxl",                    "Fast SDXL", SupportsImage: true),
                new ModelSeed("fal-ai/stable-diffusion-v35-large",   "Stable Diffusion 3.5 Large", SupportsImage: true),
                new ModelSeed("fal-ai/ideogram/v2",                  "Ideogram v2", SupportsImage: true),
                new ModelSeed("fal-ai/flux/dev/image-to-image",      "FLUX Dev Img2Img", SupportsImage: true),
                new ModelSeed("fal-ai/flux-pro/kontext",             "FLUX Pro Kontext Img2Img", SupportsImage: true),
                // --- Video models ---
                new ModelSeed("fal-ai/kling-video/v2.1/standard/text-to-video",   "Kling 2.1 Text-to-Video", SupportsVideo: true),
                new ModelSeed("fal-ai/kling-video/v2.1/standard/image-to-video",  "Kling 2.1 Image-to-Video", SupportsVideo: true),
                new ModelSeed("fal-ai/kling-video/v1.6/pro/text-to-video",        "Kling 1.6 Pro Text-to-Video", SupportsVideo: true),
                new ModelSeed("fal-ai/kling-video/v1.6/pro/image-to-video",       "Kling 1.6 Pro Img-to-Video", SupportsVideo: true),
                new ModelSeed("fal-ai/wan/v1.3/text-to-video",                    "WAN v1.3 Text-to-Video", SupportsVideo: true),
                new ModelSeed("fal-ai/wan/v1.3/image-to-video",                   "WAN v1.3 Image-to-Video", SupportsVideo: true),
                new ModelSeed("fal-ai/wan-i2v",                                   "WAN Image-to-Video", SupportsVideo: true),
                new ModelSeed("fal-ai/wan-t2v",                                   "WAN Text-to-Video", SupportsVideo: true),
                new ModelSeed("fal-ai/luma-dream-machine",                        "Luma Dream Machine", SupportsVideo: true),
                new ModelSeed("fal-ai/luma-dream-machine/image-to-video",         "Luma Dream Machine Img2Video", SupportsVideo: true),
                new ModelSeed("fal-ai/minimax/video-01",                          "Minimax Video-01", SupportsVideo: true),
                new ModelSeed("fal-ai/minimax/video-01-live",                     "Minimax Video-01 Live", SupportsVideo: true),
                new ModelSeed("fal-ai/cogvideox-5b",                              "CogVideoX 5B", SupportsVideo: true),
                new ModelSeed("fal-ai/hunyuan-video",                             "HunyuanVideo", SupportsVideo: true),
                new ModelSeed("fal-ai/hunyuan-video-image-to-video",              "HunyuanVideo Img2Video", SupportsVideo: true),
                new ModelSeed("fal-ai/mochi-v1",                                  "Mochi v1", SupportsVideo: true),
                new ModelSeed("fal-ai/ltx-video",                                 "LTX-Video", SupportsVideo: true),
                new ModelSeed("fal-ai/ltx-video/image-to-video",                  "LTX-Video Img2Video", SupportsVideo: true),
                // --- Pika video models (via Fal.ai) ---
                new ModelSeed("fal-ai/pika/v2.2/text-to-video",                   "Pika 2.2 Text-to-Video", SupportsVideo: true),
                new ModelSeed("fal-ai/pika/v2.2/image-to-video",                  "Pika 2.2 Image-to-Video", SupportsVideo: true),
            }),

        new ProviderSeed(
            Key: "grok",
            Name: "xAI Grok",
            BaseUrl: "https://api.x.ai/v1",
            SupportsListModels: false,
            Models: new()
            {
                new ModelSeed("grok-2-image-1212", "Grok 2 Aurora (gratuito)", SupportsImage: true),
                new ModelSeed("grok-2-aurora",     "Grok Aurora", SupportsImage: true),
            }),

        new ProviderSeed(
            Key: "huggingface",
            Name: "HuggingFace",
            BaseUrl: "https://api-inference.huggingface.co",
            SupportsListModels: false,
            Models: new()
            {
                // FLUX family (best open-source)
                new ModelSeed("black-forest-labs/FLUX.1-schnell",        "FLUX.1 Schnell (gratuito, rápido)", SupportsImage: true),
                new ModelSeed("black-forest-labs/FLUX.1-dev",            "FLUX.1 Dev", SupportsImage: true),
                new ModelSeed("black-forest-labs/FLUX.1-kontext-dev",    "FLUX.1 Kontext Dev", SupportsImage: true),
                new ModelSeed("black-forest-labs/FLUX.2-dev",            "FLUX.2 Dev", SupportsImage: true),
                new ModelSeed("black-forest-labs/FLUX.2-max",            "FLUX.2 Max", SupportsImage: true),
                new ModelSeed("black-forest-labs/FLUX.2-flex",           "FLUX.2 Flex", SupportsImage: true),

                // Stable Diffusion family
                new ModelSeed("stabilityai/stable-diffusion-xl-base-1.0",        "SDXL Base 1.0 (gratuito)", SupportsImage: true),
                new ModelSeed("stabilityai/stable-diffusion-xl-refiner-1.0",     "SDXL Refiner", SupportsImage: true),
                new ModelSeed("runwayml/stable-diffusion-v1-5",                  "Stable Diffusion v1.5 (gratuito)", SupportsImage: true),
                new ModelSeed("stabilityai/stable-diffusion-3-medium-diffusers", "SD3 Medium (gratuito)", SupportsImage: true),
                new ModelSeed("stabilityai/stable-diffusion-3.5-large",          "SD 3.5 Large", SupportsImage: true),
                new ModelSeed("stabilityai/stable-diffusion-3.5-medium",         "SD 3.5 Medium", SupportsImage: true),
                new ModelSeed("stabilityai/stable-diffusion-3.5-large-turbo",    "SD 3.5 Large Turbo", SupportsImage: true),

                // ByteDance Hyper-SD (very fast)
                new ModelSeed("ByteDance/Hyper-SD",    "Hyper-SD (gratuito, muito rápido)", SupportsImage: true),
                new ModelSeed("ByteDance/Hyper-SDXL",  "Hyper-SDXL (gratuito, rápido)", SupportsImage: true),

                // DeepFloyd IF (great for text in images)
                new ModelSeed("deepfloyd/IF-I-XL-v1.0", "DeepFloyd IF XL", SupportsImage: true),
                new ModelSeed("deepfloyd/IF",            "DeepFloyd IF (texto em imagens)", SupportsImage: true),

                // Qwen Image (Alibaba)
                new ModelSeed("Qwen/Qwen-Image",      "Qwen Image", SupportsImage: true),
                new ModelSeed("Qwen/Qwen-Image-2512", "Qwen Image 2512", SupportsImage: true),
                new ModelSeed("Qwen/Qwen-Image-Edit", "Qwen Image Edit", SupportsImage: true),

                // Z-Image (Tongyi-MAI / Alibaba)
                new ModelSeed("Tongyi-MAI/Z-Image",       "Z-Image", SupportsImage: true),
                new ModelSeed("Tongyi-MAI/Z-Image-Turbo", "Z-Image Turbo", SupportsImage: true),
                new ModelSeed("Tongyi-MAI/Z-Image-Edit",  "Z-Image Edit", SupportsImage: true),

                // --- Text generation (Serverless Inference API) ---
                new ModelSeed("meta-llama/Meta-Llama-3.1-8B-Instruct",  "LLaMA 3.1 8B Instruct (gratuito)"),
                new ModelSeed("meta-llama/Meta-Llama-3.1-70B-Instruct", "LLaMA 3.1 70B Instruct"),
                new ModelSeed("Qwen/Qwen2.5-72B-Instruct",              "Qwen 2.5 72B Instruct"),
                new ModelSeed("mistralai/Mistral-7B-Instruct-v0.3",     "Mistral 7B Instruct v0.3 (gratuito)"),
                new ModelSeed("microsoft/Phi-3.5-mini-instruct",        "Phi 3.5 Mini Instruct (gratuito)"),
                new ModelSeed("google/gemma-2-9b-it",                   "Gemma 2 9B Instruct (gratuito)"),

                // GLM Image (ZhipuAI)
                new ModelSeed("zai-org/glm-image", "GLM Image (ZhipuAI)", SupportsImage: true),
                new ModelSeed("THUDM/glm-image",   "GLM Image (THUDM)", SupportsImage: true),

                // Tencent Hunyuan Image
                new ModelSeed("tencent/Hunyuan-DiT",    "Hunyuan DiT", SupportsImage: true),
                new ModelSeed("tencent/HunyuanImage-2.1","HunyuanImage 2.1", SupportsImage: true),
                new ModelSeed("tencent/HunyuanImage-3", "HunyuanImage 3", SupportsImage: true),
                new ModelSeed("tencent/HunyuanImage",   "HunyuanImage", SupportsImage: true),

                // HiDream Image
                new ModelSeed("HiDream-ai/HiDream-I1",      "HiDream I1", SupportsImage: true),
                new ModelSeed("HiDream-ai/HiDream-I1-Fast", "HiDream I1 Fast", SupportsImage: true),
                new ModelSeed("HiDream-ai/HiDream-I1-Dev",  "HiDream I1 Dev", SupportsImage: true),

                // Video via HF
                new ModelSeed("stabilityai/stable-video-diffusion-img2vid-xt", "Stable Video Diffusion XT", SupportsVideo: true),
                new ModelSeed("stabilityai/stable-video-diffusion",            "Stable Video Diffusion", SupportsVideo: true),
                new ModelSeed("stabilityai/stable-video-diffusion-img2vid",    "Stable Video Diffusion Img2Vid", SupportsVideo: true),
                new ModelSeed("stabilityai/svd",                               "SVD (Stable Video Diffusion)", SupportsVideo: true),
                new ModelSeed("THUDM/CogVideoX-5B",                            "CogVideoX 5B", SupportsVideo: true),
                new ModelSeed("THUDM/CogVideoX-2B",                            "CogVideoX 2B", SupportsVideo: true),
                new ModelSeed("THUDM/CogVideoX-1.5",                           "CogVideoX 1.5", SupportsVideo: true),
                new ModelSeed("Lightricks/LTX-Video",                          "LTX-Video (Lightricks)", SupportsVideo: true),
                new ModelSeed("Wan-AI/Wan2.1-T2V-14B",                         "WAN 2.1 Text-to-Video 14B", SupportsVideo: true),
                new ModelSeed("Wan-AI/Wan2.1-I2V-14B",                         "WAN 2.1 Image-to-Video 14B", SupportsVideo: true),
                new ModelSeed("Wan-AI/Wan2.1-T2V-1.3B",                        "WAN 2.1 Text-to-Video 1.3B", SupportsVideo: true),
                new ModelSeed("VideoCrafter/VideoCrafter2",                     "VideoCrafter 2", SupportsVideo: true),
                new ModelSeed("VideoCrafter/VideoCrafter1",                     "VideoCrafter 1", SupportsVideo: true),
                new ModelSeed("THUDM/VideoCrafter2",                            "VideoCrafter 2 (THUDM)", SupportsVideo: true),
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
                new ModelSeed("black-forest-labs/flux-schnell",                       "FLUX Schnell (gratuito)", SupportsImage: true),
                new ModelSeed("black-forest-labs/flux-1-schnell:free",                "FLUX 1 Schnell Free", SupportsImage: true),
                new ModelSeed("black-forest-labs/flux-1-dev:free",                    "FLUX 1 Dev (gratuito)", SupportsImage: true),
                new ModelSeed("stabilityai/stable-diffusion-xl-base-1.0:free",        "SDXL Base (gratuito)", SupportsImage: true),

                // --- Paid image models ---
                new ModelSeed("black-forest-labs/flux-1.1-pro",                       "FLUX 1.1 Pro", SupportsImage: true),
                new ModelSeed("black-forest-labs/flux-dev",                           "FLUX Dev", SupportsImage: true),
                new ModelSeed("black-forest-labs/flux-kontext-pro",                   "FLUX Kontext Pro", SupportsImage: true),
                new ModelSeed("stability-ai/stable-diffusion-3.5-large",              "SD 3.5 Large", SupportsImage: true),
                new ModelSeed("google/gemini-2.5-flash-image",                        "Nano Banana (Gemini 2.5 Flash)", SupportsImage: true),
                new ModelSeed("google/gemini-3.1-flash-image-preview",                "Nano Banana 2 (Gemini 3.1 Flash)", SupportsImage: true),
                new ModelSeed("google/gemini-3-pro-image-preview",                    "Nano Banana Pro (Gemini 3 Pro)", SupportsImage: true),
                new ModelSeed("google/imagen-3",                                      "Google Imagen 3", SupportsImage: true),
                new ModelSeed("openai/gpt-image-1",                                   "GPT Image 1 (via OpenRouter)", SupportsImage: true),
            }),

        new ProviderSeed(
            Key: "replicate",
            Name: "Replicate (Gratuito)",
            BaseUrl: "https://api.replicate.com/v1",
            SupportsListModels: false,
            Models: new()
            {
                // Image generation (free tier available)
                new ModelSeed("black-forest-labs/flux-schnell",                    "FLUX Schnell (gratuito)", SupportsImage: true),
                new ModelSeed("black-forest-labs/flux-pro",                        "FLUX Pro", SupportsImage: true),
                new ModelSeed("stability-ai/stable-diffusion-3.5-large",           "Stable Diffusion 3.5 Large", SupportsImage: true),
                new ModelSeed("stability-ai/stable-diffusion-3-medium",            "Stable Diffusion 3 Medium (gratuito)", SupportsImage: true),
                new ModelSeed("stability-ai/stable-diffusion-xl",                  "SDXL (gratuito)", SupportsImage: true),
                new ModelSeed("openai/dall-e-3",                                   "DALL-E 3 (via Replicate)", SupportsImage: true),
                // Text-to-video (free tier available)
                new ModelSeed("cjwbw/frame-interpolation",                         "Frame Interpolation", SupportsVideo: true),
                new ModelSeed("deforum-art/deforum-stable-diffusion",              "Deforum Stable Diffusion", SupportsVideo: true),
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
                new ModelSeed("black-forest-labs/flux-1-schnell-fp8",              "FLUX Schnell (gratuito)", SupportsImage: true),
                new ModelSeed("black-forest-labs/flux-1-dev-fp8",                  "FLUX Dev (gratuito)", SupportsImage: true),
                new ModelSeed("stabilityai/stable-diffusion-xl-base-1.0",          "SDXL Base (gratuito)", SupportsImage: true),
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
                new ModelSeed("stable-diffusion",                                   "Stable Diffusion (local image gen)", SupportsImage: true),
            }),

        new ProviderSeed(
            Key: "runway",
            Name: "Runway ML",
            BaseUrl: "https://api.dev.runwayml.com/v1",
            SupportsListModels: false,
            Models: new()
            {
                new ModelSeed("gen4_turbo",  "Runway Gen-4 Turbo (Txt/Img-to-Video)", SupportsVideo: true),
                new ModelSeed("gen4",        "Runway Gen-4 (Txt/Img-to-Video)", SupportsVideo: true),
                new ModelSeed("gen3a_turbo", "Runway Gen-3 Alpha Turbo", SupportsVideo: true),
            }),

        new ProviderSeed(
            Key: "shotstack",
            Name: "Shotstack",
            BaseUrl: "https://api.shotstack.io/v1",
            SupportsListModels: false,
            Models: new()
            {
                new ModelSeed("shotstack", "Shotstack Composition (Text-to-Video)", SupportsVideo: true),
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

            var hasVideoModels = seed.Models.Any(m => m.SupportsVideo);

            if (existingProvider == null)
            {
                var provider = new AiProvider(seed.Key, seed.Name, seed.SupportsListModels, seed.BaseUrl);
                provider.Enable();
                if (hasVideoModels) provider.SetVideoProvider(true);
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
                // Update IsVideoProvider flag on existing provider if needed
                if (hasVideoModels)
                {
                    var existingEntity = db.AiProviders.Find(providerId);
                    if (existingEntity != null && !existingEntity.IsVideoProvider)
                    {
                        existingEntity.SetVideoProvider(true);
                        db.SaveChanges();
                    }
                }
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

                    // Populate capabilities if image or video support is declared
                    if (modelSeed.SupportsImage || modelSeed.SupportsVideo)
                    {
                        var capabilities = new
                        {
                            supportsImage = modelSeed.SupportsImage,
                            supportsVideo = modelSeed.SupportsVideo,
                            supportsText = true  // Always true by default
                        };
                        var capabilitiesJson = JsonSerializer.Serialize(capabilities);
                        model.UpdateDetails(modelSeed.DisplayName, null, null, null, capabilitiesJson);
                    }

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

        // --- Final pass: ensure admin group has access to ALL providers and models ---
        // This fixes cases where models were added before access grants were applied.
        if (adminGroup != null)
        {
            var allProviderIds = db.AiProviders.Select(p => p.Id).ToList();
            var allModelIds = db.AiModels.Select(m => m.Id).ToList();

            var currentProviderAccesses = db.GroupAiProviderAccesses
                .Where(a => a.GroupId == adminGroup.Id)
                .Select(a => a.ProviderId)
                .ToHashSet();

            var currentModelAccesses = db.GroupAiModelAccesses
                .Where(a => a.GroupId == adminGroup.Id)
                .Select(a => a.AiModelId)
                .ToHashSet();

            int finalProviderGrants = 0, finalModelGrants = 0;

            foreach (var pid in allProviderIds)
            {
                if (!currentProviderAccesses.Contains(pid))
                {
                    db.GroupAiProviderAccesses.Add(new GroupAiProviderAccess(adminGroup.Id, pid));
                    finalProviderGrants++;
                }
            }

            foreach (var mid in allModelIds)
            {
                if (!currentModelAccesses.Contains(mid))
                {
                    db.GroupAiModelAccesses.Add(new GroupAiModelAccess(adminGroup.Id, mid));
                    finalModelGrants++;
                }
            }

            if (finalProviderGrants > 0 || finalModelGrants > 0)
            {
                db.SaveChanges();
                logger?.LogInformation(
                    "[AiDataSeeder] Final pass: granted {PA} missing provider accesses, {MA} missing model accesses to admin group.",
                    finalProviderGrants, finalModelGrants);
            }
        }

        logger?.LogInformation(
            "[AiDataSeeder] Done. Added {Providers} providers, {Models} models. " +
            "Granted {PA} provider accesses, {MA} model accesses to admin group.",
            addedProviders, addedModels, grantedProviderAccess, grantedModelAccess);
    }

    private record ProviderSeed(string Key, string Name, string? BaseUrl, bool SupportsListModels, List<ModelSeed> Models);
    private record ModelSeed(string Key, string DisplayName, bool SupportsImage = false, bool SupportsVideo = false);
}
