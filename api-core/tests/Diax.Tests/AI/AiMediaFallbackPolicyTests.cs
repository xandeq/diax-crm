using Diax.Application.AI.Services;
using Diax.Domain.AI;

namespace Diax.Tests.AI;

public class AiMediaFallbackPolicyTests
{
    [Theory]
    [InlineData(AiErrorCategory.QuotaExhausted)]
    [InlineData(AiErrorCategory.RateLimit)]
    [InlineData(AiErrorCategory.Timeout)]
    [InlineData(AiErrorCategory.ProviderUnavailable)]
    [InlineData(AiErrorCategory.AuthFailed)]
    [InlineData(AiErrorCategory.ModelNotFound)]
    [InlineData(AiErrorCategory.ConfigurationMissing)]
    [InlineData(AiErrorCategory.Unknown)]
    public void ShouldFallback_ReturnsTrue_ForTransientOrStructuralProviderErrors(string category)
    {
        Assert.True(AiMediaFallbackPolicy.ShouldFallback(category));
    }

    [Theory]
    [InlineData(AiErrorCategory.InvalidRequest)]
    [InlineData(AiErrorCategory.CapabilityMismatch)]
    public void ShouldFallback_ReturnsFalse_ForUserRequestErrors(string category)
    {
        // Erro estrutural do pedido: repetir em outro provider produziria o mesmo erro.
        Assert.False(AiMediaFallbackPolicy.ShouldFallback(category));
    }

    [Fact]
    public void ImageProviderOrder_ContainsOnlyProvidersWithRegisteredClients()
    {
        // Keys precisam bater com ProviderName dos clients registrados no DI
        var registeredImageClients = new[] { "openai", "openrouter", "gemini", "falai", "grok", "huggingface" };
        foreach (var key in AiMediaFallbackPolicy.ImageProviderOrder)
            Assert.Contains(key, registeredImageClients);
    }

    [Fact]
    public void VideoProviderOrder_ContainsOnlyProvidersWithRegisteredClients()
    {
        // Runway fica fora (exige imagem de referência); Shotstack fora (composição, não T2V)
        var registeredVideoClients = new[] { "falai", "huggingface", "runway", "replicate", "shotstack", "wavespeed", "modelslab" };
        foreach (var key in AiMediaFallbackPolicy.VideoProviderOrder)
            Assert.Contains(key, registeredVideoClients);
    }

    [Fact]
    public void VideoProviderOrder_PutsCheapProvidersBeforeSlowFreeTier()
    {
        // wavespeed/modelslab (~$0.05, ~30-60s) vêm antes do huggingface (grátis mas cold start de minutos)
        var order = AiMediaFallbackPolicy.VideoProviderOrder.ToList();
        Assert.True(order.IndexOf("wavespeed") < order.IndexOf("huggingface"));
        Assert.True(order.IndexOf("modelslab") < order.IndexOf("huggingface"));
    }

    [Fact]
    public void PreferredModels_ExistForEveryProviderInFallbackOrder()
    {
        foreach (var key in AiMediaFallbackPolicy.ImageProviderOrder)
            Assert.True(AiMediaFallbackPolicy.PreferredImageModel.ContainsKey(key),
                $"Provider '{key}' na cadeia de imagem sem modelo preferido definido.");

        foreach (var key in AiMediaFallbackPolicy.VideoProviderOrder)
            Assert.True(AiMediaFallbackPolicy.PreferredVideoModel.ContainsKey(key),
                $"Provider '{key}' na cadeia de vídeo sem modelo preferido definido.");
    }

    [Fact]
    public void FallbackOrders_HaveNoDuplicates()
    {
        Assert.Equal(AiMediaFallbackPolicy.ImageProviderOrder.Count,
            AiMediaFallbackPolicy.ImageProviderOrder.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(AiMediaFallbackPolicy.VideoProviderOrder.Count,
            AiMediaFallbackPolicy.VideoProviderOrder.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }
}
