using Diax.Application.AI.Services;

namespace Diax.Tests.AI;

public class AiGenerationCostEstimatorTests
{
    [Fact]
    public void EstimateVideoCostUsd_WaveSpeed_ChargesPerSecondWithFiveSecondMinimum()
    {
        Assert.Equal(0.05m, AiGenerationCostEstimator.EstimateVideoCostUsd(
            "wavespeed", "wavespeed-ai/wan-2.2/t2v-480p-ultra-fast", 3)); // mínimo de 5s
        Assert.Equal(0.10m, AiGenerationCostEstimator.EstimateVideoCostUsd(
            "wavespeed", "wavespeed-ai/wan-2.2/t2v-480p-ultra-fast", 10));
    }

    [Fact]
    public void EstimateVideoCostUsd_HuggingFace_IsFree()
    {
        Assert.Equal(0m, AiGenerationCostEstimator.EstimateVideoCostUsd(
            "huggingface", "Wan-AI/Wan2.1-T2V-1.3B", 5));
    }

    [Fact]
    public void EstimateVideoCostUsd_UnknownProvider_ReturnsNull()
    {
        Assert.Null(AiGenerationCostEstimator.EstimateVideoCostUsd("desconhecido", "x", 5));
        Assert.Null(AiGenerationCostEstimator.EstimateVideoCostUsd(null, null, 5));
    }

    [Fact]
    public void EstimateImageCostUsd_FreeTierProviders_ReturnZero()
    {
        Assert.Equal(0m, AiGenerationCostEstimator.EstimateImageCostUsd("gemini", "gemini-2.5-flash-image", 1));
        Assert.Equal(0m, AiGenerationCostEstimator.EstimateImageCostUsd("grok", "grok-2-image-1212", 2));
        Assert.Equal(0m, AiGenerationCostEstimator.EstimateImageCostUsd("huggingface", "black-forest-labs/FLUX.1-schnell", 1));
        Assert.Equal(0m, AiGenerationCostEstimator.EstimateImageCostUsd("pollinations", "flux", 3));
    }

    [Fact]
    public void EstimateImageCostUsd_PaidModels_MultiplyByCount()
    {
        Assert.Equal(0.08m, AiGenerationCostEstimator.EstimateImageCostUsd("openai", "dall-e-3", 2));
        Assert.Equal(0.04m, AiGenerationCostEstimator.EstimateImageCostUsd("gemini", "imagen-4.0-generate-001", 1));
    }

    [Fact]
    public void EstimateImageCostUsd_UnknownModel_ReturnsNull()
    {
        Assert.Null(AiGenerationCostEstimator.EstimateImageCostUsd("openrouter", "google/gemini-2.5-flash-image", 1));
    }
}
