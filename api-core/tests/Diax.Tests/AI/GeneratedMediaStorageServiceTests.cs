using System.Net;
using Diax.Infrastructure.Ai;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Diax.Tests.AI;

public class GeneratedMediaStorageServiceTests : IDisposable
{
    // PNG 1x1 válido
    private const string TinyPngBase64 =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

    private readonly string _tempRoot;

    public GeneratedMediaStorageServiceTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"diax-media-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempRoot);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempRoot, recursive: true); } catch { /* best effort */ }
    }

    [Fact]
    public async Task TrySaveImageAsync_SavesDataUri_AndReturnsRelativeUrl()
    {
        var service = CreateService();

        var mediaId = Guid.NewGuid();
        var url = await service.TrySaveImageAsync(
            $"data:image/png;base64,{TinyPngBase64}", isBase64: true, mediaId);

        Assert.NotNull(url);
        Assert.Equal($"/generated-media/{mediaId:N}.png", url);
        Assert.True(File.Exists(Path.Combine(_tempRoot, "generated-media", $"{mediaId:N}.png")));
    }

    [Fact]
    public async Task TrySaveImageAsync_SavesRawBase64_DefaultingToPng()
    {
        var service = CreateService();

        var mediaId = Guid.NewGuid();
        var url = await service.TrySaveImageAsync(TinyPngBase64, isBase64: true, mediaId);

        Assert.NotNull(url);
        Assert.EndsWith(".png", url);
    }

    [Fact]
    public async Task TrySaveImageAsync_DownloadsProviderUrl()
    {
        var pngBytes = Convert.FromBase64String(TinyPngBase64);
        var service = CreateService(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(pngBytes)
            };
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
            return response;
        });

        var mediaId = Guid.NewGuid();
        var url = await service.TrySaveImageAsync(
            "https://provider.example.com/image.png", isBase64: false, mediaId);

        Assert.NotNull(url);
        var savedFile = Path.Combine(_tempRoot, "generated-media", $"{mediaId:N}.png");
        Assert.True(File.Exists(savedFile));
        Assert.Equal(pngBytes, await File.ReadAllBytesAsync(savedFile));
    }

    [Fact]
    public async Task TrySaveImageAsync_ReturnsNull_WhenDownloadFails()
    {
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var url = await service.TrySaveImageAsync(
            "https://provider.example.com/expired.png", isBase64: false, Guid.NewGuid());

        Assert.Null(url);
    }

    [Fact]
    public async Task TrySaveImageAsync_ReturnsNull_ForInvalidBase64_WithoutThrowing()
    {
        var service = CreateService();

        var url = await service.TrySaveImageAsync("isso não é base64!!!", isBase64: true, Guid.NewGuid());

        Assert.Null(url);
    }

    [Fact]
    public async Task TrySaveImageAsync_ReturnsNull_ForEmptyContent()
    {
        var service = CreateService();

        Assert.Null(await service.TrySaveImageAsync("", isBase64: true, Guid.NewGuid()));
        Assert.Null(await service.TrySaveImageAsync("   ", isBase64: false, Guid.NewGuid()));
    }

    private GeneratedMediaStorageService CreateService(
        Func<HttpRequestMessage, HttpResponseMessage>? responseFactory = null)
    {
        var httpClient = new HttpClient(new StubHandler(
            responseFactory ?? (_ => new HttpResponseMessage(HttpStatusCode.NotFound))));

        var env = new Mock<IWebHostEnvironment>();
        env.SetupGet(e => e.WebRootPath).Returns(_tempRoot);
        env.SetupGet(e => e.ContentRootPath).Returns(_tempRoot);

        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(a => a.HttpContext).Returns((HttpContext?)null); // sem request → URL relativa

        return new GeneratedMediaStorageService(
            httpClient, env.Object, accessor.Object,
            NullLogger<GeneratedMediaStorageService>.Instance);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _factory;
        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> factory) => _factory = factory;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_factory(request));
    }
}
