using Diax.Infrastructure.Ai;
using Diax.Shared.Ai;

namespace Diax.Tests.AI;

public class RunwayVideoClientTests
{
    private static VideoGenerationOptions Options(int w, int h, string? ar = null) => new(
        ApiKey: "key", BaseUrl: "", Model: "gen4_turbo",
        Width: w, Height: h, AspectRatio: ar);

    [Theory]
    [InlineData(1280, 720, null, "1280:720")]   // paisagem 16:9
    [InlineData(720, 1280, null, "720:1280")]   // retrato 9:16
    [InlineData(960, 960, null, "960:960")]     // quadrado
    [InlineData(1920, 1080, null, "1280:720")]  // 16:9 em outra resolução
    [InlineData(1024, 768, null, "1104:832")]   // 4:3 → mais próximo
    public void MapToSupportedRatio_Gen4_PicksNearestValidRatio(int w, int h, string? ar, string expected)
    {
        Assert.Equal(expected, RunwayVideoClient.MapToSupportedRatio(Options(w, h, ar), isGen4: true));
    }

    [Theory]
    [InlineData("16:9", "1280:720")]
    [InlineData("9:16", "720:1280")]
    [InlineData("1:1", "960:960")]
    [InlineData("21:9", "1584:672")]
    public void MapToSupportedRatio_Gen4_AspectRatioStringTakesPriority(string ar, string expected)
    {
        // width/height contradizem o aspectRatio de propósito — o aspectRatio explícito vence
        Assert.Equal(expected, RunwayVideoClient.MapToSupportedRatio(Options(100, 100, ar), isGen4: true));
    }

    [Theory]
    [InlineData("16:9", "1280:768")]
    [InlineData("9:16", "768:1280")]
    public void MapToSupportedRatio_Gen3_UsesGen3Ratios(string ar, string expected)
    {
        Assert.Equal(expected, RunwayVideoClient.MapToSupportedRatio(Options(1280, 720, ar), isGen4: false));
    }

    [Fact]
    public void MapToSupportedRatio_InvalidAspectRatioString_FallsBackToDimensions()
    {
        Assert.Equal("720:1280",
            RunwayVideoClient.MapToSupportedRatio(Options(720, 1280, "invalido"), isGen4: true));
    }
}
