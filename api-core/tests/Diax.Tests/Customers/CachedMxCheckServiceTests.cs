using Diax.Application.Customers;
using Diax.Application.Customers.Services;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Diax.Tests.Customers;

public class CachedMxCheckServiceTests
{
    private readonly Mock<IMxLookupService> _lookupMock = new();
    private readonly Mock<IMxCacheRepository> _cacheMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    public CachedMxCheckServiceTests()
    {
        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    private CachedMxCheckService CreateSut(ExtractorPullOptions? options = null) => new(
        _lookupMock.Object,
        _cacheMock.Object,
        _unitOfWorkMock.Object,
        Options.Create(options ?? new ExtractorPullOptions()),
        Mock.Of<ILogger<CachedMxCheckService>>());

    private void SetupCache(params MxCacheEntry[] entries)
    {
        var dict = entries.ToDictionary(e => e.Domain, e => e, StringComparer.Ordinal);
        _cacheMock
            .Setup(c => c.GetByDomainsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyDictionary<string, MxCacheEntry>)dict);
    }

    [Fact]
    public async Task CheckManyAsync_JunkDomain_ReturnsNoMxWithoutLookup()
    {
        SetupCache();
        var sut = CreateSut();

        var result = await sut.CheckManyAsync(new[] { "instagram.local" });

        Assert.Equal(MxCheckResult.NoMx, result["instagram.local"]);
        _lookupMock.Verify(l => l.CheckAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CheckManyAsync_FreshValidCache_ReturnsCachedWithoutLookup()
    {
        var entry = new MxCacheEntry("fresh-valid.com", (int)MxCheckResult.Valid, DateTime.UtcNow.AddDays(-1));
        SetupCache(entry);
        var sut = CreateSut();

        var result = await sut.CheckManyAsync(new[] { "fresh-valid.com" });

        Assert.Equal(MxCheckResult.Valid, result["fresh-valid.com"]);
        _lookupMock.Verify(l => l.CheckAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CheckManyAsync_ExpiredValidCache_CallsLookupAndRefreshes()
    {
        var entry = new MxCacheEntry("stale-valid.com", (int)MxCheckResult.Valid, DateTime.UtcNow.AddDays(-31));
        SetupCache(entry);
        _lookupMock
            .Setup(l => l.CheckAsync("stale-valid.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MxCheckResult.Valid);
        var sut = CreateSut();

        var result = await sut.CheckManyAsync(new[] { "stale-valid.com" });

        Assert.Equal(MxCheckResult.Valid, result["stale-valid.com"]);
        _lookupMock.Verify(l => l.CheckAsync("stale-valid.com", It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.UpdateAsync(entry, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal((int)MxCheckResult.Valid, entry.ResultCode);
    }

    [Fact]
    public async Task CheckManyAsync_FreshUnverifiedCache_ReturnsCachedWithoutLookup()
    {
        var entry = new MxCacheEntry("recent-unverified.com", (int)MxCheckResult.Unverified, DateTime.UtcNow.AddHours(-2));
        SetupCache(entry);
        var sut = CreateSut();

        var result = await sut.CheckManyAsync(new[] { "recent-unverified.com" });

        Assert.Equal(MxCheckResult.Unverified, result["recent-unverified.com"]);
        _lookupMock.Verify(l => l.CheckAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CheckManyAsync_ExpiredUnverifiedCache_CallsLookup()
    {
        // TTL de Unverified é 24h, não 30 dias — 25h atrás já expirou.
        var entry = new MxCacheEntry("old-unverified.com", (int)MxCheckResult.Unverified, DateTime.UtcNow.AddHours(-25));
        SetupCache(entry);
        _lookupMock
            .Setup(l => l.CheckAsync("old-unverified.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MxCheckResult.Valid);
        var sut = CreateSut();

        var result = await sut.CheckManyAsync(new[] { "old-unverified.com" });

        Assert.Equal(MxCheckResult.Valid, result["old-unverified.com"]);
        _lookupMock.Verify(l => l.CheckAsync("old-unverified.com", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckManyAsync_NoCacheEntry_CallsLookupOnceAndPersistsNewEntry()
    {
        SetupCache();
        _lookupMock
            .Setup(l => l.CheckAsync("newdomain.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MxCheckResult.Valid);
        var sut = CreateSut();

        var result = await sut.CheckManyAsync(new[] { "newdomain.com" });

        Assert.Equal(MxCheckResult.Valid, result["newdomain.com"]);
        _lookupMock.Verify(l => l.CheckAsync("newdomain.com", It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.AddAsync(
            It.Is<MxCacheEntry>(e => e.Domain == "newdomain.com" && e.ResultCode == (int)MxCheckResult.Valid),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckManyAsync_SameDomainRepeated5Times_CallsLookupOnce()
    {
        SetupCache();
        _lookupMock
            .Setup(l => l.CheckAsync("repeated.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MxCheckResult.Valid);
        var sut = CreateSut();

        var domains = Enumerable.Repeat("repeated.com", 5).ToList();
        var result = await sut.CheckManyAsync(domains);

        Assert.Single(result);
        Assert.Equal(MxCheckResult.Valid, result["repeated.com"]);
        _lookupMock.Verify(l => l.CheckAsync("repeated.com", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckManyAsync_MxCheckDisabled_ReturnsUnverifiedForAllWithoutLookupOrCacheWrite()
    {
        SetupCache();
        var sut = CreateSut(new ExtractorPullOptions { MxCheckEnabled = false });

        var result = await sut.CheckManyAsync(new[] { "domain1.com", "domain2.com" });

        Assert.Equal(MxCheckResult.Unverified, result["domain1.com"]);
        Assert.Equal(MxCheckResult.Unverified, result["domain2.com"]);
        _lookupMock.Verify(l => l.CheckAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _cacheMock.Verify(c => c.AddAsync(It.IsAny<MxCacheEntry>(), It.IsAny<CancellationToken>()), Times.Never);
        _cacheMock.Verify(c => c.UpdateAsync(It.IsAny<MxCacheEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CheckManyAsync_EmptyList_ReturnsEmptyDictionaryWithoutAnyCalls()
    {
        var sut = CreateSut();

        var result = await sut.CheckManyAsync(Array.Empty<string>());

        Assert.Empty(result);
        _lookupMock.Verify(l => l.CheckAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _cacheMock.Verify(c => c.GetByDomainsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CheckManyAsync_LookupReturnsUnverified_IsPersistedInCache()
    {
        SetupCache();
        _lookupMock
            .Setup(l => l.CheckAsync("timeout.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MxCheckResult.Unverified);
        var sut = CreateSut();

        var result = await sut.CheckManyAsync(new[] { "timeout.com" });

        Assert.Equal(MxCheckResult.Unverified, result["timeout.com"]);
        _cacheMock.Verify(c => c.AddAsync(
            It.Is<MxCacheEntry>(e => e.Domain == "timeout.com" && e.ResultCode == (int)MxCheckResult.Unverified),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
