using System.Text;
using Diax.Application.ErrorLogs;
using Diax.Application.ErrorLogs.Dtos;
using Diax.Domain.ApiKeys;
using Diax.Domain.ErrorLogs;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Diax.Tests.Application.ErrorLogs;

/// <summary>
/// Testes de regressão para bugs Altos (A-01..A-06) do AUDIT_REPORT_V2.
/// </summary>
public class ErrorLogServiceHighTests
{
    private static IMemoryCache BuildCache() => new MemoryCache(new MemoryCacheOptions());

    private static (ApiKey key, string plain) MakeKey(string scope = "error-logs.ingest", string name = "investiq")
    {
        var (key, plain) = ApiKey.Create(name, "system", 365, scope);
        return (key, plain);
    }

    private ErrorLogService BuildSvc(
        Mock<IErrorLogRepository>? repoMock = null,
        Mock<IApiKeyRepository>? keyRepoMock = null,
        IMemoryCache? cache = null)
    {
        repoMock ??= new Mock<IErrorLogRepository>();
        keyRepoMock ??= new Mock<IApiKeyRepository>();
        cache ??= BuildCache();
        return new ErrorLogService(repoMock.Object, keyRepoMock.Object, cache,
            NullLogger<ErrorLogService>.Instance);
    }

    // ─── A-01: Rate limiter thread-safe ──────────────────────────────────────

    [Fact]
    public async Task A01_RateLimit_AtomicIncrement_BlocksAt51_NotAt49()
    {
        var (key, plain) = MakeKey();
        var repoMock = new Mock<IErrorLogRepository>();
        var keyRepoMock = new Mock<IApiKeyRepository>();

        keyRepoMock.Setup(r => r.GetByKeyHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(key);
        repoMock.Setup(r => r.IncrementOccurrenceAtomicAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
        repoMock.Setup(r => r.AddAsync(It.IsAny<ErrorLog>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

        var cache = BuildCache();
        var svc = BuildSvc(repoMock, keyRepoMock, cache);

        var req = new IngestErrorLogRequest
        {
            AppName = "investiq",
            Level = "Error",
            Message = "test"
        };

        // 50 requests devem passar
        for (var i = 0; i < 50; i++)
        {
            var r = await svc.IngestAsync(plain, req);
            Assert.True(r.IsSuccess, $"Request {i + 1} deveria passar mas falhou: {r.Error.Message}");
        }

        // 51ª request deve ser bloqueada
        var blocked = await svc.IngestAsync(plain, req);
        Assert.False(blocked.IsSuccess);
        Assert.Contains("RateLimited", blocked.Error.Code);
    }

    [Fact]
    public async Task A01_RateLimit_BatchOf100_CountsCorrectly()
    {
        var (key, plain) = MakeKey();
        var keyRepoMock = new Mock<IApiKeyRepository>();
        keyRepoMock.Setup(r => r.GetByKeyHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(key);

        var svc = BuildSvc(keyRepoMock: keyRepoMock);

        var batchOf51 = new BatchIngestErrorLogRequest
        {
            Logs = Enumerable.Range(0, 51)
                .Select(_ => new IngestErrorLogRequest { AppName = "x", Level = "Error", Message = "m" })
                .ToList()
        };

        var result = await svc.IngestBatchAsync(plain, batchOf51);
        Assert.False(result.IsSuccess);
        Assert.Contains("RateLimited", result.Error.Code);
    }

    // ─── A-03: Scope match exato — sem bypass de substring ───────────────────

    [Fact]
    public async Task A03_ScopeCheck_ExactMatch_Passes()
    {
        var (key, plain) = MakeKey(scope: "error-logs.ingest");
        var keyRepoMock = new Mock<IApiKeyRepository>();
        keyRepoMock.Setup(r => r.GetByKeyHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(key);

        var repoMock = new Mock<IErrorLogRepository>();
        repoMock.Setup(r => r.IncrementOccurrenceAtomicAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
        repoMock.Setup(r => r.AddAsync(It.IsAny<ErrorLog>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

        var svc = BuildSvc(repoMock, keyRepoMock);
        var result = await svc.IngestAsync(plain, new IngestErrorLogRequest
            { AppName = "investiq", Level = "Error", Message = "test" });

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task A03_ScopeCheck_SubstringScope_IsRejected()
    {
        // Antes do fix: "error-logs.ingest-admin".Contains("error-logs.ingest") == true → bypass
        // Depois do fix: split + exact match → rejeitado
        var (key, plain) = MakeKey(scope: "error-logs.ingest-admin");
        var keyRepoMock = new Mock<IApiKeyRepository>();
        keyRepoMock.Setup(r => r.GetByKeyHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(key);

        var svc = BuildSvc(keyRepoMock: keyRepoMock);
        var result = await svc.IngestAsync(plain, new IngestErrorLogRequest
            { AppName = "x", Level = "Error", Message = "test" });

        Assert.False(result.IsSuccess);
        Assert.Contains("Forbidden", result.Error.Code);
    }

    [Fact]
    public async Task A03_ScopeCheck_MultipleScopes_MatchesCorrectOne()
    {
        var (key, plain) = MakeKey(scope: "blog.write,error-logs.ingest,customers.read");
        var keyRepoMock = new Mock<IApiKeyRepository>();
        keyRepoMock.Setup(r => r.GetByKeyHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(key);

        var repoMock = new Mock<IErrorLogRepository>();
        repoMock.Setup(r => r.IncrementOccurrenceAtomicAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
        repoMock.Setup(r => r.AddAsync(It.IsAny<ErrorLog>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

        var svc = BuildSvc(repoMock, keyRepoMock);
        var result = await svc.IngestAsync(plain, new IngestErrorLogRequest
            { AppName = "x", Level = "Error", Message = "test" });

        Assert.True(result.IsSuccess); // scope válido dentro da lista
    }

    // ─── A-04: PII scrubbing em todos os campos ───────────────────────────────

    [Theory]
    [InlineData("/api/users/096.613.297-10/profile", "[CPF_REDACTED]")]
    [InlineData("/api/customers/foo@bar.com/data", "[EMAIL_REDACTED]")]
    public void A04_ScrubPii_RequestPath_RemovesPii(string path, string expectedReplacement)
    {
        var scrubbed = ErrorLogService.ScrubPiiPublic(path);
        Assert.Contains(expectedReplacement, scrubbed);
        Assert.DoesNotContain("096.613.297-10", scrubbed);
        Assert.DoesNotContain("foo@bar.com", scrubbed);
    }

    [Fact]
    public void A04_ScrubPii_ExceptionMessage_RemovesEmail()
    {
        var exType = "UserNotFoundException: user@company.com not found";
        var scrubbed = ErrorLogService.ScrubPiiPublic(exType);
        Assert.Contains("[EMAIL_REDACTED]", scrubbed);
        Assert.DoesNotContain("user@company.com", scrubbed);
    }

    [Fact]
    public void A04_ScrubPii_BearerToken_Removed()
    {
        var msg = "Auth failed: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.abc.xyz in header";
        var scrubbed = ErrorLogService.ScrubPiiPublic(msg);
        Assert.Contains("[TOKEN_REDACTED]", scrubbed);
        Assert.DoesNotContain("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9", scrubbed);
    }

    [Fact]
    public void A04_ScrubPii_ConnectionString_Removed()
    {
        var msg = "DB error: password=SuperSecret123 in connection";
        var scrubbed = ErrorLogService.ScrubPiiPublic(msg);
        Assert.Contains("[REDACTED]", scrubbed);
        Assert.DoesNotContain("SuperSecret123", scrubbed);
    }

    // ─── A-05: OccurredAt clamp ──────────────────────────────────────────────

    [Fact]
    public async Task A05_OccurredAt_FutureDate_IsClamped()
    {
        var (key, plain) = MakeKey();
        var keyRepoMock = new Mock<IApiKeyRepository>();
        keyRepoMock.Setup(r => r.GetByKeyHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(key);

        ErrorLog? captured = null;
        var repoMock = new Mock<IErrorLogRepository>();
        repoMock.Setup(r => r.IncrementOccurrenceAtomicAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
        repoMock.Setup(r => r.AddAsync(It.IsAny<ErrorLog>(), It.IsAny<CancellationToken>()))
                .Callback<ErrorLog, CancellationToken>((l, _) => captured = l)
                .Returns(Task.CompletedTask);
        repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

        var svc = BuildSvc(repoMock, keyRepoMock);
        var futureDate = DateTime.UtcNow.AddYears(1);
        await svc.IngestAsync(plain, new IngestErrorLogRequest
        {
            AppName = "x", Level = "Error", Message = "test",
            OccurredAt = futureDate
        });

        Assert.NotNull(captured);
        // OccurredAt deve ser próximo de agora, não do futuro
        Assert.True(captured!.OccurredAt <= DateTime.UtcNow.AddMinutes(6),
            $"OccurredAt não foi clampado: {captured.OccurredAt}");
    }

    [Fact]
    public async Task A05_OccurredAt_VeryOldDate_IsClamped()
    {
        var (key, plain) = MakeKey();
        var keyRepoMock = new Mock<IApiKeyRepository>();
        keyRepoMock.Setup(r => r.GetByKeyHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(key);

        ErrorLog? captured = null;
        var repoMock = new Mock<IErrorLogRepository>();
        repoMock.Setup(r => r.IncrementOccurrenceAtomicAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
        repoMock.Setup(r => r.AddAsync(It.IsAny<ErrorLog>(), It.IsAny<CancellationToken>()))
                .Callback<ErrorLog, CancellationToken>((l, _) => captured = l)
                .Returns(Task.CompletedTask);
        repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

        var svc = BuildSvc(repoMock, keyRepoMock);
        await svc.IngestAsync(plain, new IngestErrorLogRequest
        {
            AppName = "x", Level = "Error", Message = "test",
            OccurredAt = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        Assert.NotNull(captured);
        Assert.True(captured!.OccurredAt >= DateTime.UtcNow.AddDays(-1),
            $"OccurredAt muito antigo não foi clampado: {captured.OccurredAt}");
    }

    // ─── A-06: AppName derivado da ApiKey ────────────────────────────────────

    [Fact]
    public async Task A06_AppName_DerivedFromApiKey_NotFromRequest()
    {
        // ApiKey criada com name="investiq" — request tenta postar como "vaganagringa"
        var (key, plain) = MakeKey(scope: "error-logs.ingest", name: "investiq");
        var keyRepoMock = new Mock<IApiKeyRepository>();
        keyRepoMock.Setup(r => r.GetByKeyHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(key);

        ErrorLog? captured = null;
        var repoMock = new Mock<IErrorLogRepository>();
        repoMock.Setup(r => r.IncrementOccurrenceAtomicAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
        repoMock.Setup(r => r.AddAsync(It.IsAny<ErrorLog>(), It.IsAny<CancellationToken>()))
                .Callback<ErrorLog, CancellationToken>((l, _) => captured = l)
                .Returns(Task.CompletedTask);
        repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

        var svc = BuildSvc(repoMock, keyRepoMock);

        // Cliente malicioso envia AppName diferente do nome da key
        await svc.IngestAsync(plain, new IngestErrorLogRequest
        {
            AppName = "vaganagringa", // ← tentativa de injeção cross-app
            Level = "Error",
            Message = "test"
        });

        Assert.NotNull(captured);
        // AppName deve ser "investiq" (nome da key), não "vaganagringa" (do request)
        Assert.Equal("investiq", captured!.AppName);
        Assert.NotEqual("vaganagringa", captured.AppName);
    }
}
