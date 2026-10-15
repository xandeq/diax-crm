using Diax.Application.EmailMarketing;
using Diax.Application.SurfAlerts;
using Diax.Application.SurfAlerts.Dtos;
using Diax.Domain.ApiKeys;
using Diax.Domain.SurfAlerts;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Diax.Tests.Application.SurfAlerts;

public class SurfAlertServiceTests
{
    private static IMemoryCache BuildCache() => new MemoryCache(new MemoryCacheOptions());

    private static (ApiKey key, string plain) CreateKeyWithScope(string scope)
        => ApiKey.Create("perplexity-computer", "system", 365, scope);

    private static IngestSurfAlertRequest BasicRequest(bool hasGoodWindow = true) => new()
    {
        Source = "perplexity-computer",
        Type = "surf_forecast_weekend",
        GeneratedAt = DateTimeOffset.UtcNow,
        Weekend = new WeekendDto
        {
            Saturday = new DateOnly(2026, 10, 17),
            Sunday = new DateOnly(2026, 10, 18)
        },
        HasGoodWindow = hasGoodWindow,
        Summary = "Melhor janela: sábado 06:00-09:00 na Barra do Jucu.",
        BestWindow = new BestWindowDto { Date = new DateOnly(2026, 10, 17), Spot = "Barra do Jucu", Rating = 4 }
    };

    private static SurfAlertService BuildService(
        out Mock<ISurfAlertRepository> repoMock,
        out Mock<IApiKeyRepository> keyRepoMock,
        out Mock<IEmailOpsAlerter> alerterMock,
        IMemoryCache? cache = null)
    {
        repoMock = new Mock<ISurfAlertRepository>();
        keyRepoMock = new Mock<IApiKeyRepository>();
        alerterMock = new Mock<IEmailOpsAlerter>();

        repoMock.Setup(r => r.AddAsync(It.IsAny<SurfAlert>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return new SurfAlertService(
            repoMock.Object, keyRepoMock.Object, alerterMock.Object,
            cache ?? BuildCache(), NullLogger<SurfAlertService>.Instance);
    }

    [Fact]
    public async Task IngestAsync_WithMissingApiKey_ReturnsUnauthorized()
    {
        var svc = BuildService(out _, out _, out _);

        var result = await svc.IngestAsync("", null, BasicRequest());

        Assert.False(result.IsSuccess);
        Assert.Contains("Unauthorized", result.Error.Code);
    }

    [Fact]
    public async Task IngestAsync_WithUnknownApiKey_ReturnsUnauthorized()
    {
        var svc = BuildService(out _, out var keyRepoMock, out _);
        keyRepoMock.Setup(r => r.GetByKeyHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApiKey?)null);

        var result = await svc.IngestAsync("dk_live_fake", null, BasicRequest());

        Assert.False(result.IsSuccess);
        Assert.Contains("Unauthorized", result.Error.Code);
    }

    [Fact]
    public async Task IngestAsync_WithKeyMissingScope_ReturnsForbidden()
    {
        var svc = BuildService(out _, out var keyRepoMock, out _);
        var (key, plain) = CreateKeyWithScope("error-logs.ingest"); // escopo errado
        keyRepoMock.Setup(r => r.GetByKeyHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(key);

        var result = await svc.IngestAsync(plain, null, BasicRequest());

        Assert.False(result.IsSuccess);
        Assert.Contains("Forbidden", result.Error.Code);
    }

    [Fact]
    public async Task IngestAsync_WithDisabledKey_ReturnsUnauthorized()
    {
        var svc = BuildService(out _, out var keyRepoMock, out _);
        var (key, plain) = CreateKeyWithScope("surf-alerts.ingest");
        key.Disable();
        keyRepoMock.Setup(r => r.GetByKeyHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(key);

        var result = await svc.IngestAsync(plain, null, BasicRequest());

        Assert.False(result.IsSuccess);
        Assert.Contains("Unauthorized", result.Error.Code);
    }

    [Fact]
    public async Task IngestAsync_WithValidKeyAndNewAlert_CreatesAndReturnsCreated()
    {
        var svc = BuildService(out var repoMock, out var keyRepoMock, out _);
        var (key, plain) = CreateKeyWithScope("surf-alerts.ingest");
        keyRepoMock.Setup(r => r.GetByKeyHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(key);

        var result = await svc.IngestAsync(plain, "surf-2026-10-17", BasicRequest());

        Assert.True(result.IsSuccess);
        Assert.Equal("created", result.Value.Status);
        repoMock.Verify(r => r.AddAsync(It.IsAny<SurfAlert>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IngestAsync_WhenHasGoodWindow_NotifiesAlerter()
    {
        var svc = BuildService(out _, out var keyRepoMock, out var alerterMock);
        var (key, plain) = CreateKeyWithScope("surf-alerts.ingest");
        keyRepoMock.Setup(r => r.GetByKeyHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(key);

        await svc.IngestAsync(plain, null, BasicRequest(hasGoodWindow: true));

        alerterMock.Verify(a => a.NotifyAsync(
            It.Is<string>(k => k.StartsWith("surf-alert:")),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IngestAsync_WhenNoGoodWindow_DoesNotNotify()
    {
        var svc = BuildService(out _, out var keyRepoMock, out var alerterMock);
        var (key, plain) = CreateKeyWithScope("surf-alerts.ingest");
        keyRepoMock.Setup(r => r.GetByKeyHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(key);

        await svc.IngestAsync(plain, null, BasicRequest(hasGoodWindow: false));

        alerterMock.Verify(a => a.NotifyAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IngestAsync_WithKnownIdempotencyKey_ReturnsExistingWithoutDuplicating()
    {
        var svc = BuildService(out var repoMock, out var keyRepoMock, out _);
        var (key, plain) = CreateKeyWithScope("surf-alerts.ingest");
        keyRepoMock.Setup(r => r.GetByKeyHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(key);

        var existing = SurfAlert.Create(
            "perplexity-computer", "surf_forecast_weekend", DateTime.UtcNow,
            new DateOnly(2026, 10, 17), new DateOnly(2026, 10, 18), true, "janela boa",
            null, null, null, null, "{}", "surf-2026-10-17");

        repoMock.Setup(r => r.GetByIdempotencyKeyAsync("surf-2026-10-17", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await svc.IngestAsync(plain, "surf-2026-10-17", BasicRequest());

        Assert.True(result.IsSuccess);
        Assert.Equal("duplicate", result.Value.Status);
        Assert.Equal(existing.Id, result.Value.Id);
        repoMock.Verify(r => r.AddAsync(It.IsAny<SurfAlert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IngestAsync_WithSundayBeforeSaturday_ReturnsValidationError()
    {
        var svc = BuildService(out _, out var keyRepoMock, out _);
        var (key, plain) = CreateKeyWithScope("surf-alerts.ingest");
        keyRepoMock.Setup(r => r.GetByKeyHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(key);

        var request = BasicRequest();
        request.Weekend = new WeekendDto
        {
            Saturday = new DateOnly(2026, 10, 18),
            Sunday = new DateOnly(2026, 10, 17)
        };

        var result = await svc.IngestAsync(plain, null, request);

        Assert.False(result.IsSuccess);
        Assert.Contains("Validation", result.Error.Code);
    }

    [Fact]
    public async Task GetRecentAsync_WithNullApiKey_SkipsAuthAndReturnsItems()
    {
        var svc = BuildService(out var repoMock, out _, out _);
        repoMock.Setup(r => r.GetRecentAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SurfAlert>)new List<SurfAlert>());

        var result = await svc.GetRecentAsync(null, 10);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task GetRecentAsync_WithApiKeyMissingViewScope_ReturnsForbidden()
    {
        var svc = BuildService(out _, out var keyRepoMock, out _);
        var (key, plain) = CreateKeyWithScope("surf-alerts.ingest"); // sem 'view'
        keyRepoMock.Setup(r => r.GetByKeyHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(key);

        var result = await svc.GetRecentAsync(plain, 10);

        Assert.False(result.IsSuccess);
        Assert.Contains("Forbidden", result.Error.Code);
    }

    [Fact]
    public async Task GetRecentAsync_WithValidViewScopeApiKey_ReturnsItems()
    {
        var svc = BuildService(out var repoMock, out var keyRepoMock, out _);
        var (key, plain) = CreateKeyWithScope("surf-alerts.view");
        keyRepoMock.Setup(r => r.GetByKeyHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(key);
        repoMock.Setup(r => r.GetRecentAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SurfAlert>)new List<SurfAlert>());

        var result = await svc.GetRecentAsync(plain, 5);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task IngestAsync_ExceedingRateLimit_ReturnsRateLimited()
    {
        var cache = BuildCache();
        var svc = BuildService(out _, out var keyRepoMock, out _, cache);
        var (key, plain) = CreateKeyWithScope("surf-alerts.ingest");
        keyRepoMock.Setup(r => r.GetByKeyHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(key);

        for (var i = 0; i < 30; i++)
        {
            var r = await svc.IngestAsync(plain, $"surf-key-{i}", BasicRequest());
            Assert.True(r.IsSuccess);
        }

        var blocked = await svc.IngestAsync(plain, "surf-key-overflow", BasicRequest());

        Assert.False(blocked.IsSuccess);
        Assert.Contains("RateLimited", blocked.Error.Code);
    }
}
