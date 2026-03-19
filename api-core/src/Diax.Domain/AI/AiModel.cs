using Diax.Domain.Common;

namespace Diax.Domain.AI;

public class AiModel : AuditableEntity
{
    public Guid ProviderId { get; private set; }
    public string ModelKey { get; private set; }
    public string DisplayName { get; private set; }
    public bool IsEnabled { get; private set; }
    public bool IsDiscovered { get; private set; }
    public decimal? InputCostHint { get; private set; }
    public decimal? OutputCostHint { get; private set; }
    public int? MaxTokensHint { get; private set; }
    public string? CapabilitiesJson { get; private set; }

    // Video generation metadata
    public bool IsActive { get; private set; } = false; // Whether model is available for use
    public int? MaxDurationSeconds { get; private set; } // Maximum video duration in seconds
    public string? MaxResolution { get; private set; } // e.g., "1080p", "4K", "720p"
    public string? SupportedAspectRatios { get; private set; } // Comma-separated: "16:9,9:16,1:1"

    // Navigation property
    public AiProvider Provider { get; private set; }

    public AiModel(Guid providerId, string modelKey, string displayName, bool isDiscovered)
    {
        ProviderId = providerId;
        ModelKey = modelKey;
        DisplayName = displayName;
        IsDiscovered = isDiscovered;
        IsEnabled = false; // Default disabled
        IsActive = false;
    }

    public void UpdateDetails(string displayName, decimal? inputCost, decimal? outputCost, int? maxTokens, string? capabilities)
    {
        DisplayName = displayName;
        InputCostHint = inputCost;
        OutputCostHint = outputCost;
        MaxTokensHint = maxTokens;
        CapabilitiesJson = capabilities;
    }

    public void UpdateVideoConstraints(int? maxDurationSeconds, string? maxResolution, string? supportedAspectRatios)
    {
        MaxDurationSeconds = maxDurationSeconds;
        MaxResolution = maxResolution;
        SupportedAspectRatios = supportedAspectRatios;
    }

    public void UpdateDisplayName(string displayName)
    {
        if (!string.IsNullOrWhiteSpace(displayName))
            DisplayName = displayName;
    }

    public void UpdateModelKey(string modelKey)
    {
        if (!string.IsNullOrWhiteSpace(modelKey))
            ModelKey = modelKey;
    }

    public void Enable() => IsEnabled = true;
    public void Disable() => IsEnabled = false;
    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    // Well-known image generation model keys (fallback when CapabilitiesJson is not configured)
    private static readonly HashSet<string> KnownImageModelKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        // OpenAI
        "dall-e-3", "dall-e-2", "gpt-image-1",

        // Google Gemini / Imagen — dedicated image generation models (via generateContent + responseModalities or :predict)
        "gemini-2.5-flash-image",                       // Gemini 2.5 Flash Image (dedicated image gen)
        "gemini-3.1-flash-image-preview",               // Gemini 3.1 Flash Image Preview
        "gemini-3-pro-image-preview",                   // Gemini 3 Pro Image Preview
        "imagen-4.0-generate-001",                      // Imagen 4.0 (paid plan required)
        "imagen-4.0-ultra-generate-001",                // Imagen 4.0 Ultra
        "imagen-4.0-fast-generate-001",                 // Imagen 4.0 Fast
        // Legacy image gen model names (kept for backward compat)
        "imagen-3.0-generate-002",
        "imagen-3.0-fast-generate-001",
        "gemini-2.0-flash-exp-image-generation",
        "gemini-2.0-flash-preview-image-generation",
        // Gemini chat models with image generation support via :generateContent + responseModalities: ["TEXT", "IMAGE"]
        // NOTE: Gemini doesn't have separate image-gen models; all chat models can generate images
        "gemini-2.5-flash",                             // Gemini 2.5 Flash (text + image gen)
        "gemini-2.0-flash",                             // Gemini 2.0 Flash (text + image gen)
        "gemini-2.5-pro",                               // Gemini 2.5 Pro (text + image gen)
        "gemini-2.0-pro",                               // Gemini 2.0 Pro (text + image gen)
        "gemini-1.5-pro",                               // Gemini 1.5 Pro (text + image gen)
        "gemini-1.5-flash",                             // Gemini 1.5 Flash (text + image gen)
        "gemini-3-flash-preview",                       // Gemini 3 Flash Preview (text + image gen)
        "gemini-3-pro-preview",                         // Gemini 3 Pro Preview (text + image gen)

        // OpenRouter — FLUX family
        "black-forest-labs/flux-1.1-pro",
        "black-forest-labs/flux-kontext-pro",
        "black-forest-labs/flux-dev",
        "black-forest-labs/flux-schnell",
        "stability-ai/stable-diffusion-3.5-large",

        // OpenRouter — Gemini image models (img2img via chat/completions)
        "google/gemini-2.0-flash-image-generation",
        "google/gemini-2.5-flash-image",
        "google/gemini-2.5-flash-preview-image-generation",

        // OpenRouter — Imagen and GPT Image
        "google/imagen-3",
        "openai/gpt-image-1",

        // Fal.ai — text-to-image models
        "fal-ai/flux/dev",
        "fal-ai/flux/schnell",
        "fal-ai/flux-pro",
        "fal-ai/flux-pro/v1.1",
        "fal-ai/flux-realism",
        "fal-ai/fast-sdxl",
        "fal-ai/stable-diffusion-v35-large",
        "fal-ai/ideogram/v2",

        // Fal.ai — img2img models (accept image_url + prompt)
        "fal-ai/flux/dev/image-to-image",
        "fal-ai/flux-pro/kontext",          // Best for img2img editing
        "fal-ai/flux-kontext/dev",          // Kontext dev variant
        "fal-ai/fast-sdxl/image-to-image",
        "fal-ai/ideogram/v2/remix",
        // Note: fal-ai/luma-dream-machine is a VIDEO model — intentionally excluded

        // Grok / xAI — image generation (free tier available)
        "grok-2-image-1212",
        "grok-2-aurora",
        "aurora",

        // HuggingFace — free text-to-image models (FLUX)
        "black-forest-labs/FLUX.1-schnell",
        "black-forest-labs/FLUX.1-dev",
        "black-forest-labs/FLUX.1-kontext-dev",
        "black-forest-labs/FLUX.2-dev",
        "black-forest-labs/FLUX.2-max",
        "black-forest-labs/FLUX.2-flex",

        // HuggingFace — Stable Diffusion family
        "stabilityai/stable-diffusion-xl-base-1.0",
        "stabilityai/stable-diffusion-xl-refiner-1.0",
        "runwayml/stable-diffusion-v1-5",
        "stabilityai/stable-diffusion-3-medium-diffusers",
        "stabilityai/stable-diffusion-3.5-large",
        "stabilityai/stable-diffusion-3.5-medium",
        "stabilityai/stable-diffusion-3.5-large-turbo",

        // HuggingFace — ByteDance Hyper-SD (very fast)
        "ByteDance/Hyper-SD",
        "ByteDance/Hyper-SDXL",

        // HuggingFace — DeepFloyd IF (great for text in images)
        "deepfloyd/IF-I-XL-v1.0",
        "deepfloyd/IF",

        // HuggingFace — Qwen Image
        "Qwen/Qwen-Image",
        "Qwen/Qwen-Image-2512",
        "Qwen/Qwen-Image-Edit",

        // HuggingFace — Z-Image (Tongyi-MAI / Alibaba)
        "Tongyi-MAI/Z-Image",
        "Tongyi-MAI/Z-Image-Turbo",
        "Tongyi-MAI/Z-Image-Edit",

        // HuggingFace — GLM Image (ZhipuAI)
        "zai-org/glm-image",
        "THUDM/glm-image",

        // HuggingFace — Tencent Hunyuan Image
        "tencent/Hunyuan-DiT",
        "tencent/HunyuanImage-2.1",
        "tencent/HunyuanImage-3",
        "tencent/HunyuanImage",

        // HuggingFace — HiDream Image
        "HiDream-ai/HiDream-I1",
        "HiDream-ai/HiDream-I1-Fast",
        "HiDream-ai/HiDream-I1-Dev",
    };

    // Well-known video generation model keys (text-to-video and image-to-video via fal.ai)
    private static readonly HashSet<string> KnownVideoModelKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        // Fal.ai — Kling video models
        "fal-ai/kling-video/v2.1/standard/text-to-video",
        "fal-ai/kling-video/v2.1/standard/image-to-video",
        "fal-ai/kling-video/v1.6/pro/text-to-video",
        "fal-ai/kling-video/v1.6/pro/image-to-video",
        "fal-ai/kling-video/v1.5/pro/text-to-video",
        "fal-ai/kling-video/v1.5/pro/image-to-video",

        // Fal.ai — WAN video models
        "fal-ai/wan/v1.3/text-to-video",
        "fal-ai/wan/v1.3/image-to-video",
        "fal-ai/wan-i2v",
        "fal-ai/wan-t2v",

        // Fal.ai — Luma Dream Machine
        "fal-ai/luma-dream-machine",
        "fal-ai/luma-dream-machine/image-to-video",
        "fal-ai/luma-photon",
        "fal-ai/luma-photon/flash",

        // Fal.ai — Minimax video
        "fal-ai/minimax/video-01",
        "fal-ai/minimax/video-01-live",

        // Fal.ai — CogVideoX
        "fal-ai/cogvideox-5b",

        // Fal.ai — other video models
        "fal-ai/hunyuan-video",
        "fal-ai/hunyuan-video-image-to-video",
        "fal-ai/mochi-v1",

        // Fal.ai — LTX Video (Lightricks)
        "fal-ai/ltx-video",
        "fal-ai/ltx-video/image-to-video",

        // Fal.ai — CogVideoX (THUDM)
        "fal-ai/cogvideox-5b-img2vid",

        // HuggingFace / HF-based — Stable Video Diffusion
        "stabilityai/stable-video-diffusion-img2vid",
        "stabilityai/stable-video-diffusion-img2vid-xt",
        "stabilityai/stable-video-diffusion",
        "stabilityai/svd-xt",

        // HuggingFace — CogVideoX
        "THUDM/CogVideoX-5B",
        "THUDM/CogVideoX-2B",
        "THUDM/CogVideoX-1.5",

        // HuggingFace — LTX-Video (Lightricks)
        "Lightricks/LTX-Video",

        // HuggingFace — Stable Video Diffusion (short aliases)
        "stabilityai/svd",
        "stabilityai/stable-video-diffusion-img2vid",

        // HuggingFace — Wan video models
        "Wan-AI/Wan2.1-T2V-14B",
        "Wan-AI/Wan2.1-I2V-14B",
        "Wan-AI/Wan2.1-T2V-1.3B",

        // HuggingFace — VideoCrafter
        "VideoCrafter/VideoCrafter2",
        "VideoCrafter/VideoCrafter1",
        "THUDM/VideoCrafter2",

        // Fal.ai — Pika video models
        "fal-ai/pika/v2.2/text-to-video",
        "fal-ai/pika/v2.2/image-to-video",
        "fal-ai/pika-labs/v2.2/text-to-video",
        "fal-ai/pika-labs/v2.2/image-to-video",

        // Runway ML
        "gen4_turbo",
        "gen4",
        "gen3a_turbo",

        // Shotstack
        "shotstack",
    };

    public bool SupportsImageGeneration() =>
        KnownImageModelKeys.Contains(ModelKey) ||
        (!string.IsNullOrEmpty(CapabilitiesJson) &&
         CapabilitiesJson.Contains("\"supportsImage\":true", StringComparison.OrdinalIgnoreCase));

    public bool SupportsVideoGeneration() =>
        KnownVideoModelKeys.Contains(ModelKey) ||
        (!string.IsNullOrEmpty(CapabilitiesJson) &&
         CapabilitiesJson.Contains("\"supportsVideo\":true", StringComparison.OrdinalIgnoreCase));

    public bool SupportsTextGeneration() =>
        string.IsNullOrEmpty(CapabilitiesJson) ||
        CapabilitiesJson.Contains("\"supportsText\":true", StringComparison.OrdinalIgnoreCase);
}
