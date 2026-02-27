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

    // Navigation property
    public AiProvider Provider { get; private set; }

    public AiModel(Guid providerId, string modelKey, string displayName, bool isDiscovered)
    {
        ProviderId = providerId;
        ModelKey = modelKey;
        DisplayName = displayName;
        IsDiscovered = isDiscovered;
        IsEnabled = false; // Default disabled
    }

    public void UpdateDetails(string displayName, decimal? inputCost, decimal? outputCost, int? maxTokens, string? capabilities)
    {
        DisplayName = displayName;
        InputCostHint = inputCost;
        OutputCostHint = outputCost;
        MaxTokensHint = maxTokens;
        CapabilitiesJson = capabilities;
    }

    public void Enable() => IsEnabled = true;
    public void Disable() => IsEnabled = false;

    // Well-known image generation model keys (fallback when CapabilitiesJson is not configured)
    private static readonly HashSet<string> KnownImageModelKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        // OpenAI
        "dall-e-3", "dall-e-2", "gpt-image-1",

        // Google Gemini / Imagen
        "imagen-3.0-generate-002",
        "imagen-3.0-fast-generate-001",
        "gemini-2.0-flash-exp-image-generation",
        "gemini-2.0-flash-preview-image-generation",
        // Gemini 2.5 Flash image gen — use the actual Gemini API model IDs below.
        // The :generateContent endpoint accepts these with responseModalities: ["IMAGE"]
        "gemini-2.5-flash-preview-image-generation",   // if/when Google releases this exact ID
        "gemini-2.5-flash-image-generation",            // alternate naming
        "gemini-2.5-flash",                             // base 2.5 Flash (supports image gen via responseModalities)
        "gemini-2.5-flash-preview-04-17",               // dated preview release

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
    };

    public bool SupportsImageGeneration() =>
        KnownImageModelKeys.Contains(ModelKey) ||
        (!string.IsNullOrEmpty(CapabilitiesJson) &&
         CapabilitiesJson.Contains("\"supportsImage\":true", StringComparison.OrdinalIgnoreCase));

    public bool SupportsTextGeneration() =>
        string.IsNullOrEmpty(CapabilitiesJson) ||
        CapabilitiesJson.Contains("\"supportsText\":true", StringComparison.OrdinalIgnoreCase);
}
