using Diax.Domain.AI;

namespace Diax.Application.AI.Services;

/// <summary>
/// Política de fallback automático entre providers de geração de mídia.
/// Regra do produto: NUNCA parar de gerar por erro transitório de um provider —
/// trocar automaticamente para o próximo provider configurado.
/// </summary>
public static class AiMediaFallbackPolicy
{
    /// <summary>
    /// Ordem de fallback para geração de imagem (gratuitos primeiro).
    /// </summary>
    public static readonly IReadOnlyList<string> ImageProviderOrder = new[]
    {
        "gemini", "grok", "falai", "openai", "openrouter", "huggingface"
    };

    /// <summary>
    /// Ordem de fallback para geração de vídeo (baratos/rápidos primeiro).
    /// wavespeed (~US$0,05/vídeo) e modelslab (~US$0,05) na frente de huggingface
    /// (grátis mas cold start de minutos) e replicate.
    /// Runway fica fora da cadeia automática: gen4_turbo/gen4.5 exigem imagem de referência.
    /// Shotstack fica fora: é composição de vídeo, não text-to-video.
    /// </summary>
    public static readonly IReadOnlyList<string> VideoProviderOrder = new[]
    {
        "falai", "wavespeed", "modelslab", "huggingface", "replicate"
    };

    /// <summary>
    /// Modelo default por provider ao cair no fallback (preferência por modelos gratuitos/rápidos).
    /// Se o modelo preferido não estiver habilitado, o serviço usa o primeiro modelo
    /// habilitado do provider com a capability necessária.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> PreferredImageModel =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["gemini"] = "gemini-2.5-flash-image",
            ["grok"] = "grok-2-image-1212",
            ["falai"] = "fal-ai/flux/schnell",
            ["openai"] = "dall-e-3",
            ["openrouter"] = "google/gemini-2.5-flash-image",
            ["huggingface"] = "black-forest-labs/FLUX.1-schnell",
        };

    public static readonly IReadOnlyDictionary<string, string> PreferredVideoModel =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["falai"] = "fal-ai/ltx-video",
            ["wavespeed"] = "wavespeed-ai/wan-2.2/t2v-480p-ultra-fast",
            ["modelslab"] = "wan2.2",
            ["huggingface"] = "Wan-AI/Wan2.1-T2V-1.3B",
            ["replicate"] = "stability-ai/stable-video-diffusion",
        };

    /// <summary>
    /// Categorias de erro que justificam tentar outro provider.
    /// InvalidRequest e CapabilityMismatch são estruturais do pedido — repetir em
    /// outro provider produziria o mesmo erro ou resultado inesperado.
    /// </summary>
    private static readonly HashSet<string> FallbackEligibleCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        AiErrorCategory.QuotaExhausted,
        AiErrorCategory.RateLimit,
        AiErrorCategory.Timeout,
        AiErrorCategory.ProviderUnavailable,
        AiErrorCategory.AuthFailed,
        AiErrorCategory.ModelNotFound,
        AiErrorCategory.ConfigurationMissing,
        AiErrorCategory.Unknown,
    };

    public static bool ShouldFallback(string errorCategory) =>
        FallbackEligibleCategories.Contains(errorCategory);
}
