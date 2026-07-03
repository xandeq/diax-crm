namespace Diax.Application.AI.Services;

/// <summary>
/// Estimativa de custo (USD) por geração de mídia, para transparência na UI.
/// Valores verificados nas páginas oficiais de pricing em 2026-07 — são ESTIMATIVAS
/// (resolução/qualidade podem alterar o valor real). null = custo desconhecido;
/// 0 = free tier do provider.
/// </summary>
public static class AiGenerationCostEstimator
{
    /// <summary>Custo estimado de um vídeo. durationSeconds é a duração pedida.</summary>
    public static decimal? EstimateVideoCostUsd(string? providerKey, string? modelKey, int? durationSeconds)
    {
        if (providerKey == null) return null;
        var duration = Math.Max(durationSeconds ?? 5, 1);

        switch (providerKey.ToLowerInvariant())
        {
            case "wavespeed":
                // $0.01/s, cobrança mínima de 5s (pricing oficial)
                return 0.01m * Math.Max(duration, 5);

            case "modelslab":
                // WAN $0.03–0.08/vídeo — usa o ponto médio
                return 0.05m;

            case "huggingface":
                return 0m; // Serverless Inference free tier (com rate limit)

            case "magichour":
                return 0m; // free tier com créditos diários renováveis (esgotou → fallback)

            case "falai" when modelKey != null:
                var mk = modelKey.ToLowerInvariant();
                if (mk.Contains("ltx-video")) return 0.04m;                    // ~$0.04/vídeo
                if (mk.Contains("kling") && mk.Contains("standard")) return 0.07m * duration / 5m;
                if (mk.Contains("kling") && mk.Contains("pro")) return 0.24m * duration / 5m;
                return null;

            default:
                return null;
        }
    }

    /// <summary>Custo estimado de uma leva de imagens.</summary>
    public static decimal? EstimateImageCostUsd(string? providerKey, string? modelKey, int numberOfImages)
    {
        if (providerKey == null) return null;
        var count = Math.Max(numberOfImages, 1);
        var mk = modelKey?.ToLowerInvariant() ?? string.Empty;

        decimal? perImage = providerKey.ToLowerInvariant() switch
        {
            "pollinations" => 0m,                                     // keyless, 100% grátis
            "gemini" when mk.Contains("flash-image") => 0m,          // free tier da API Gemini
            "gemini" when mk.StartsWith("imagen") => 0.04m,          // Imagen 4 (pago)
            "grok" => 0m,                                             // grok-2-image no free tier atual
            "huggingface" => 0m,                                      // Serverless free tier
            "openai" when mk == "dall-e-3" => 0.04m,                  // 1024x1024 standard
            "openai" when mk == "dall-e-2" => 0.02m,
            "openai" when mk == "gpt-image-1" => 0.04m,
            "falai" when mk.Contains("schnell") => 0.003m,
            "falai" when mk.Contains("flux/dev") => 0.025m,
            "falai" when mk.Contains("flux-pro") => 0.05m,
            _ => null
        };

        return perImage.HasValue ? perImage.Value * count : null;
    }
}
