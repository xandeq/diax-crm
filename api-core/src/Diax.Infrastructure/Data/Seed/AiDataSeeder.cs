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
    /// <summary>
    /// Models confirmed broken (404/410/403) that must be DISABLED on startup.
    /// Removing from KnownProviders prevents re-enable; this list actively disables them.
    /// </summary>
    private static readonly Dictionary<string, HashSet<string>> KnownBrokenModels = new()
    {
        ["huggingface"] = new(StringComparer.OrdinalIgnoreCase)
        {
            "black-forest-labs/FLUX.1-dev",              // 410 deprecated on HF router
            "black-forest-labs/FLUX.1-kontext-dev",      // 404
            "black-forest-labs/FLUX.2-dev",              // 404
            "black-forest-labs/FLUX.2-max",              // 404
            "black-forest-labs/FLUX.2-flex",             // 404
            "stabilityai/stable-diffusion-xl-refiner-1.0",     // 404
            "stabilityai/stable-diffusion-3.5-large",          // 400 endpoint error
            "stabilityai/stable-diffusion-3.5-medium",         // 404
            "stabilityai/stable-diffusion-3.5-large-turbo",    // 404
            "runwayml/stable-diffusion-v1-5",            // 404
            "ByteDance/Hyper-SD",                        // 404
            "ByteDance/Hyper-SDXL",                      // 404
            "deepfloyd/IF-I-XL-v1.0",                   // 404
            "deepfloyd/IF",                              // 404
            "Qwen/Qwen-Image",                           // 404
            "Qwen/Qwen-Image-2512",                      // 404
            "Qwen/Qwen-Image-Edit",                      // 404
            "Tongyi-MAI/Z-Image",                        // 404
            "Tongyi-MAI/Z-Image-Turbo",                  // 404
            "Tongyi-MAI/Z-Image-Edit",                   // 404
            "zai-org/glm-image",                         // 404
            "THUDM/glm-image",                           // 404
            "tencent/Hunyuan-DiT",                       // 404
            "tencent/HunyuanImage-2.1",                  // 404
            "tencent/HunyuanImage-3",                    // 404
            "tencent/HunyuanImage",                      // 404
            "HiDream-ai/HiDream-I1",                     // 404
            "HiDream-ai/HiDream-I1-Fast",                // 404
            "HiDream-ai/HiDream-I1-Dev",                 // 404
            "stabilityai/stable-video-diffusion-img2vid-xt", // 404
            "stabilityai/stable-video-diffusion",        // 404
            "stabilityai/stable-video-diffusion-img2vid",// 404
            "stabilityai/svd",                           // 404
            "THUDM/CogVideoX-1.5",                       // 404
            "VideoCrafter/VideoCrafter2",                // 404
            "VideoCrafter/VideoCrafter1",                // 404
            "THUDM/VideoCrafter2",                       // 404
        },
        ["runway"] = new(StringComparer.OrdinalIgnoreCase)
        {
            "gen4", // 403 "Model variant gen4 is not available"
        },
        ["openrouter"] = new(StringComparer.OrdinalIgnoreCase)
        {
            "black-forest-labs/flux-schnell",            // removed from OR catalog
            "black-forest-labs/flux-1-schnell:free",     // removed
            "black-forest-labs/flux-1-dev:free",         // removed
            "stabilityai/stable-diffusion-xl-base-1.0:free", // removed
            "black-forest-labs/flux-1.1-pro",            // removed
            "black-forest-labs/flux-dev",                // removed
            "black-forest-labs/flux-kontext-pro",        // removed
            "stability-ai/stable-diffusion-3.5-large",   // removed
            "google/imagen-3",                           // does not exist in OR
            "openai/gpt-image-1",                        // does not exist in OR
        },
        ["gemini"] = new(StringComparer.OrdinalIgnoreCase)
        {
            "gemini-2.5-flash",       // chat model, does NOT output images
            "gemini-2.0-flash",       // chat model
            "gemini-2.5-pro",         // chat model
            "gemini-2.0-pro",         // chat model
            "gemini-1.5-pro",         // chat model
            "gemini-1.5-flash",       // chat model
            "gemini-3-flash-preview", // chat model
            "gemini-3-pro-preview",   // chat model (wrong ID anyway)
        },
    };

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
                // Gemini native image-generation models (use :generateContent + responseModalities)
                // These are the only models that actually output images — generic chat models do NOT.
                new ModelSeed("gemini-2.5-flash-image",          "Gemini 2.5 Flash Image (gratuito)", SupportsImage: true),
                new ModelSeed("gemini-3.1-flash-image-preview",  "Gemini 3.1 Flash Image Preview", SupportsImage: true),
                new ModelSeed("gemini-3-pro-image-preview",      "Gemini 3 Pro Image Preview", SupportsImage: true),
                // Imagen 4 family (requires paid tier — Vertex AI billing)
                new ModelSeed("imagen-4.0-generate-001",         "Imagen 4.0 (pago)", SupportsImage: true),
                new ModelSeed("imagen-4.0-fast-generate-001",    "Imagen 4.0 Fast (pago)", SupportsImage: true),
                new ModelSeed("imagen-4.0-ultra-generate-001",   "Imagen 4.0 Ultra (pago)", SupportsImage: true),
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
                // ── Image models (tested & confirmed working on router.huggingface.co 2026-03) ──
                new ModelSeed("black-forest-labs/FLUX.1-schnell",        "FLUX.1 Schnell (gratuito, rápido)", SupportsImage: true),
                new ModelSeed("stabilityai/stable-diffusion-xl-base-1.0",        "SDXL Base 1.0 (gratuito)", SupportsImage: true),
                new ModelSeed("stabilityai/stable-diffusion-3-medium-diffusers", "SD3 Medium (licença no HF)", SupportsImage: true),

                // ── Text generation (Serverless Inference API) ──
                new ModelSeed("meta-llama/Meta-Llama-3.1-8B-Instruct",  "LLaMA 3.1 8B Instruct (gratuito)"),
                new ModelSeed("meta-llama/Meta-Llama-3.1-70B-Instruct", "LLaMA 3.1 70B Instruct"),
                new ModelSeed("Qwen/Qwen2.5-72B-Instruct",              "Qwen 2.5 72B Instruct"),
                new ModelSeed("mistralai/Mistral-7B-Instruct-v0.3",     "Mistral 7B Instruct v0.3 (gratuito)"),
                new ModelSeed("microsoft/Phi-3.5-mini-instruct",        "Phi 3.5 Mini Instruct (gratuito)"),
                new ModelSeed("google/gemma-2-9b-it",                   "Gemma 2 9B Instruct (gratuito)"),

                // ── Video (keep for future availability) ──
                new ModelSeed("Wan-AI/Wan2.1-T2V-14B",  "WAN 2.1 Text-to-Video 14B", SupportsVideo: true),
                new ModelSeed("Wan-AI/Wan2.1-I2V-14B",  "WAN 2.1 Image-to-Video 14B", SupportsVideo: true),
                new ModelSeed("Wan-AI/Wan2.1-T2V-1.3B", "WAN 2.1 Text-to-Video 1.3B", SupportsVideo: true),
                new ModelSeed("THUDM/CogVideoX-5B",      "CogVideoX 5B", SupportsVideo: true),
                new ModelSeed("THUDM/CogVideoX-2B",      "CogVideoX 2B", SupportsVideo: true),
                new ModelSeed("Lightricks/LTX-Video",    "LTX-Video (Lightricks)", SupportsVideo: true),
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
                // OpenRouter does NOT have /images/generations — all image generation
                // goes through /chat/completions with responseModalities.
                // FLUX/SD/Kontext models were removed from OR catalog.
                // Only confirmed image-output models as of 2026-03:

                // Google Gemini image models (via /chat/completions)
                new ModelSeed("google/gemini-2.5-flash-image",                        "Gemini 2.5 Flash Image (OR)", SupportsImage: true),
                new ModelSeed("google/gemini-3.1-flash-image-preview",                "Gemini 3.1 Flash Image Preview (OR)", SupportsImage: true),
                new ModelSeed("google/gemini-3-pro-image-preview",                    "Gemini 3 Pro Image Preview (OR)", SupportsImage: true),
                // OpenAI GPT-5 image models (via /chat/completions)
                new ModelSeed("openai/gpt-5-image-mini",                              "GPT-5 Image Mini (OR)", SupportsImage: true),
                new ModelSeed("openai/gpt-5-image",                                   "GPT-5 Image (OR)", SupportsImage: true),
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
                // gen4_turbo: confirmed working — valid ratios: 1280:720, 720:1280, 1104:832, 832:1104, 960:960, 1584:672
                new ModelSeed("gen4_turbo",  "Runway Gen-4 Turbo (Img-to-Video)", SupportsVideo: true),
                // gen4.5: confirmed working (HTTP 200)
                new ModelSeed("gen4.5",      "Runway Gen-4.5 (Img-to-Video)", SupportsVideo: true),
                new ModelSeed("gen3a_turbo", "Runway Gen-3 Alpha Turbo", SupportsVideo: true),
                // gen4 was removed — 403 "Model variant gen4 is not available"
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

        // Build full set of catalog model keys per provider key (for re-enable pass)
        var catalogModelKeysByProviderKey = KnownProviders.ToDictionary(
            p => p.Key,
            p => p.Models.Select(m => m.Key).ToHashSet(StringComparer.OrdinalIgnoreCase));

        int reEnabledModels = 0, reEnabledProviders = 0;

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
                // Re-enable provider if it was disabled (catalog providers are always valid)
                var existingEntity = db.AiProviders.Find(providerId);
                if (existingEntity != null)
                {
                    var changed = false;
                    if (!existingEntity.IsEnabled)
                    {
                        existingEntity.Enable();
                        reEnabledProviders++;
                        logger?.LogInformation("[AiDataSeeder] 🔁 Re-enabled provider: {Key}", seed.Key);
                        changed = true;
                    }
                    if (hasVideoModels && !existingEntity.IsVideoProvider)
                    {
                        existingEntity.SetVideoProvider(true);
                        changed = true;
                    }
                    if (changed) db.SaveChanges();
                }
            }

            // --- Grant provider access ---
            if (adminGroup != null && !existingProviderAccesses.Contains(providerId))
            {
                db.GroupAiProviderAccesses.Add(new GroupAiProviderAccess(adminGroup.Id, providerId));
                existingProviderAccesses.Add(providerId);
                grantedProviderAccess++;
            }

            // --- Batch-add new models + re-enable disabled catalog models ---
            var knownKeys = existingModelKeysByProvider.GetValueOrDefault(providerId) ?? new HashSet<string>();
            var catalogKeysForProvider = catalogModelKeysByProviderKey.GetValueOrDefault(seed.Key) ?? new HashSet<string>();
            var newModels = new List<AiModel>();

            // Re-enable existing models that are in the catalog but currently disabled
            var disabledCatalogModels = db.AiModels
                .Where(m => m.ProviderId == providerId && !m.IsEnabled && catalogKeysForProvider.Contains(m.ModelKey))
                .ToList();

            foreach (var disabledModel in disabledCatalogModels)
            {
                var seedEntry = seed.Models.FirstOrDefault(ms =>
                    ms.Key.Equals(disabledModel.ModelKey, StringComparison.OrdinalIgnoreCase));
                if (seedEntry == null) continue;

                disabledModel.Enable();

                // Restore capabilities if cleared
                if (string.IsNullOrEmpty(disabledModel.CapabilitiesJson) &&
                    (seedEntry.SupportsImage || seedEntry.SupportsVideo))
                {
                    var capabilities = new
                    {
                        supportsImage = seedEntry.SupportsImage,
                        supportsVideo = seedEntry.SupportsVideo,
                        supportsText = true
                    };
                    disabledModel.UpdateDetails(disabledModel.DisplayName, null, null, null,
                        JsonSerializer.Serialize(capabilities));
                }

                reEnabledModels++;
                logger?.LogInformation(
                    "[AiDataSeeder] 🔁 Re-enabled catalog model: {Key} (provider: {Provider})",
                    disabledModel.ModelKey, seed.Key);
            }

            if (disabledCatalogModels.Count > 0)
                db.SaveChanges();

            // --- Disable known-broken models (404/410/deprecated) ---
            if (KnownBrokenModels.TryGetValue(seed.Key, out var brokenKeys))
            {
                var toDisable = db.AiModels
                    .Where(m => m.ProviderId == providerId && m.IsEnabled && brokenKeys.Contains(m.ModelKey))
                    .ToList();

                foreach (var m in toDisable)
                {
                    m.Disable();
                    logger?.LogInformation(
                        "[AiDataSeeder] 🚫 Disabled known-broken model: {Key} (provider: {Provider})",
                        m.ModelKey, seed.Key);
                }

                if (toDisable.Count > 0)
                    db.SaveChanges();
            }

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
            "Re-enabled {RP} providers, {RM} models. " +
            "Granted {PA} provider accesses, {MA} model accesses to admin group.",
            addedProviders, addedModels, reEnabledProviders, reEnabledModels, grantedProviderAccess, grantedModelAccess);
    }

    private record ProviderSeed(string Key, string Name, string? BaseUrl, bool SupportsListModels, List<ModelSeed> Models);
    private record ModelSeed(string Key, string DisplayName, bool SupportsImage = false, bool SupportsVideo = false);
}
