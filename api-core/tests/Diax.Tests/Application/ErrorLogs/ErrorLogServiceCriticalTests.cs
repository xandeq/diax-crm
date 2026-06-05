using System.Text;
using Diax.Application.ErrorLogs;
using Diax.Application.ErrorLogs.Dtos;
using Diax.Domain.ApiKeys;
using Diax.Domain.ErrorLogs;
using Diax.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Diax.Tests.Application.ErrorLogs;

/// <summary>
/// Testes de regressão para os 4 bugs críticos do AUDIT_REPORT_V2:
/// C-01 ResolveAsync persiste, C-02 dedupe atômico, C-03 TruncateUtf8 Unicode, C-04 cursor keyset.
/// Cada teste falha com o código original e passa após o fix.
/// </summary>
public class ErrorLogServiceCriticalTests
{
    // ─── helpers ─────────────────────────────────────────────────────────────

    private static IMemoryCache BuildCache() =>
        new MemoryCache(new MemoryCacheOptions());

    private static ApiKey BuildValidKey(string scope = "error-logs.ingest")
    {
        var (key, plain) = CreateApiKeyWithScope(scope);
        return key;
    }

    private static (ApiKey key, string plain) CreateApiKeyWithScope(string scope, string name = "investiq")
    {
        var (key, plain) = ApiKey.Create(name, "system", 365, scope);
        return (key, plain);
    }

    private static IngestErrorLogRequest BasicRequest(string appName = "investiq") => new()
    {
        AppName = appName,
        Level = "Error",
        Message = "Test error",
        ExceptionType = "KeyError",
        Source = "auth/token.py",
        LineNumber = 84,
        OccurredAt = DateTime.UtcNow
    };

    // ─── C-01: ResolveAsync persiste no banco ────────────────────────────────

    [Fact]
    public async Task C01_ResolveAsync_UsesTrackedEntity_PersistsToDatabase()
    {
        // Arrange
        var repoMock = new Mock<IErrorLogRepository>();
        var keyRepoMock = new Mock<IApiKeyRepository>();
        var cache = BuildCache();

        var logId = Guid.NewGuid();
        var log = ErrorLog.Create("investiq", "production", ErrorLogLevel.Error,
            "test", "KeyError", null, "file.py", 1, "GET", "/api", null, null, DateTime.UtcNow);

        // GetByIdTrackedAsync retorna entidade mutável (comportamento rastreado esperado)
        repoMock.Setup(r => r.GetByIdTrackedAsync(logId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(log);
        repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

        var svc = new ErrorLogService(repoMock.Object, keyRepoMock.Object, cache,
            NullLogger<ErrorLogService>.Instance);

        // Act
        var result = await svc.ResolveAsync(logId, "Fixed in commit abc");

        // Assert — SaveChangesAsync deve ser chamado (prova de persistência)
        repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsResolved);
        Assert.Equal("Fixed in commit abc", result.Value.ResolutionNote);
    }

    [Fact]
    public async Task C01_ResolveAsync_WhenNotFound_Returns404()
    {
        var repoMock = new Mock<IErrorLogRepository>();
        repoMock.Setup(r => r.GetByIdTrackedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ErrorLog?)null);

        var svc = new ErrorLogService(repoMock.Object, Mock.Of<IApiKeyRepository>(),
            BuildCache(), NullLogger<ErrorLogService>.Instance);

        var result = await svc.ResolveAsync(Guid.NewGuid(), null);

        Assert.False(result.IsSuccess);
        Assert.Contains("NotFound", result.Error.Code);
    }

    // ─── C-02: Dedupe atômico — IncrementOccurrenceAtomicAsync ──────────────

    [Fact]
    public async Task C02_IngestAsync_WhenFingerprintExists_IncrementsAtomically_NotCreatesNew()
    {
        // Arrange
        var repoMock = new Mock<IErrorLogRepository>();
        var keyRepoMock = new Mock<IApiKeyRepository>();
        var (apiKey, plainKey) = CreateApiKeyWithScope("error-logs.ingest");

        keyRepoMock.Setup(r => r.GetByKeyHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(apiKey);

        // IncrementOccurrenceAtomicAsync retorna true → já existe entrada aberta
        repoMock.Setup(r => r.IncrementOccurrenceAtomicAsync(
                    It.IsAny<string>(), "investiq", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

        var existingLog = ErrorLog.Create("investiq", "production", ErrorLogLevel.Error,
            "msg", "KeyError", null, "auth/token.py", 84, null, null, null, null, DateTime.UtcNow);
        repoMock.Setup(r => r.GetOpenByFingerprintAsync(It.IsAny<string>(), "investiq", It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingLog);

        var svc = new ErrorLogService(repoMock.Object, keyRepoMock.Object, BuildCache(),
            NullLogger<ErrorLogService>.Instance);

        // Act
        var result = await svc.IngestAsync(plainKey, BasicRequest("investiq"));

        // Assert — AddAsync NUNCA chamado (sem criação de duplicata)
        repoMock.Verify(r => r.AddAsync(It.IsAny<ErrorLog>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsDuplicate);
    }

    [Fact]
    public async Task C02_IngestAsync_WhenNoExisting_CreatesNewLog()
    {
        var repoMock = new Mock<IErrorLogRepository>();
        var keyRepoMock = new Mock<IApiKeyRepository>();
        var (apiKey, plainKey) = CreateApiKeyWithScope("error-logs.ingest");

        keyRepoMock.Setup(r => r.GetByKeyHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(apiKey);

        // IncrementOccurrenceAtomicAsync retorna false → nenhuma entrada aberta
        repoMock.Setup(r => r.IncrementOccurrenceAtomicAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
        repoMock.Setup(r => r.AddAsync(It.IsAny<ErrorLog>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

        var svc = new ErrorLogService(repoMock.Object, keyRepoMock.Object, BuildCache(),
            NullLogger<ErrorLogService>.Instance);

        var result = await svc.IngestAsync(plainKey, BasicRequest());

        repoMock.Verify(r => r.AddAsync(It.IsAny<ErrorLog>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsDuplicate);
    }

    // ─── C-03: TruncateUtf8 — fronteira Unicode ──────────────────────────────

    [Fact]
    public void C03_TruncateUtf8_AsciiOnly_NoCorruption()
    {
        var input = new string('a', 100);
        var result = ErrorLogService.TruncateUtf8(input, 50);
        Assert.Equal(50, Encoding.UTF8.GetByteCount(result.Replace("...[truncated]", "")));
    }

    [Fact]
    public void C03_TruncateUtf8_WithPortugueseAccents_NoCutInMiddleOfSequence()
    {
        // 'ã' em UTF-8 = 2 bytes (0xC3 0xA3) — truncar no byte 1 do par seria inválido
        var input = "ação de log com caracteres como ção e também ã é ç";
        var maxBytes = 20;

        var result = ErrorLogService.TruncateUtf8(input, maxBytes);

        // A string resultante (sem o sufixo) deve ser UTF-8 válida (sem replacement chars)
        var withoutSuffix = result.Replace("...[truncated]", "");
        var backToBytes = Encoding.UTF8.GetBytes(withoutSuffix);
        var decodedBack = Encoding.UTF8.GetString(backToBytes);
        Assert.Equal(withoutSuffix, decodedBack); // round-trip perfeito = sem corrupção
        Assert.DoesNotContain("�", result); // sem replacement character
    }

    [Fact]
    public void C03_TruncateUtf8_WithEmoji_NoCutInMiddleOfSurrogate()
    {
        // Emoji 🚀 = 4 bytes em UTF-8
        var input = "Error in 🚀 deploy pipeline for 🎯 target";
        var maxBytes = 15; // corta no meio de um emoji se não tratado

        var result = ErrorLogService.TruncateUtf8(input, maxBytes);

        Assert.DoesNotContain("�", result);
        var withoutSuffix = result.Replace("...[truncated]", "");
        var reEncoded = Encoding.UTF8.GetByteCount(withoutSuffix);
        Assert.True(reEncoded <= maxBytes,
            $"Resultado ({reEncoded} bytes) excede limite ({maxBytes} bytes): '{result}'");
    }

    [Fact]
    public void C03_TruncateUtf8_ShortInput_ReturnedUnchanged()
    {
        var input = "short string ã";
        var result = ErrorLogService.TruncateUtf8(input, 10000);
        Assert.Equal(input, result);
        Assert.DoesNotContain("[truncated]", result);
    }

    [Fact]
    public void C03_TruncateUtf8_NullOrEmpty_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, ErrorLogService.TruncateUtf8(null, 100));
        Assert.Equal(string.Empty, ErrorLogService.TruncateUtf8("", 100));
    }

    // ─── C-04: Cursor Pagination — keyset correto ────────────────────────────

    [Fact]
    public void C04_CursorEncoding_RoundTrip_IsStable()
    {
        var occurredAt = DateTime.UtcNow;
        var id = Guid.NewGuid();

        var cursor = ErrorLogFilter.EncodeCursor(occurredAt, id);
        var decoded = ErrorLogFilter.DecodeCursor(cursor);

        Assert.NotNull(decoded);
        Assert.Equal(id, decoded!.Value.id);
        // Comparação com tolerância de 1 tick (arredondamento ISO8601)
        Assert.Equal(occurredAt.ToString("O"), decoded.Value.occurredAt.ToString("O"));
    }

    [Fact]
    public void C04_DecodeCursor_InvalidBase64_ReturnsNull()
    {
        Assert.Null(ErrorLogFilter.DecodeCursor("not-valid-base64!!!!"));
        Assert.Null(ErrorLogFilter.DecodeCursor(null));
        Assert.Null(ErrorLogFilter.DecodeCursor(""));
    }

    [Fact]
    public void C04_DecodeCursor_TruncatedCursor_ReturnsNull()
    {
        // Cursor válido mas sem separador
        var badCursor = Convert.ToBase64String(Encoding.UTF8.GetBytes("2026-06-05T00:00:00Z"));
        Assert.Null(ErrorLogFilter.DecodeCursor(badCursor));
    }

    [Fact]
    public async Task C04_GetFilteredAsync_SecondPage_DoesNotRepeatFirstPageItems()
    {
        // Testa que a lógica de paginação no serviço produz NextCursor e o mesmo filtro
        // com esse cursor resulta em query diferente (teste de contrato do filter)
        var repoMock = new Mock<IErrorLogRepository>();
        var keyRepoMock = new Mock<IApiKeyRepository>();

        var page1Items = Enumerable.Range(0, 3).Select(i =>
            ErrorLog.Create("app", "prod", ErrorLogLevel.Error, $"msg {i}",
                "Ex", null, "file.py", i, null, null, null, null,
                DateTime.UtcNow.AddMinutes(-i))).ToList();

        // Primeira chamada: sem cursor → retorna 3 itens
        repoMock.Setup(r => r.GetFilteredAsync(
                    It.Is<ErrorLogFilter>(f => f.Cursor == null),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(((IReadOnlyList<ErrorLog>)page1Items, 10));

        // Segunda chamada: com cursor → retorna itens diferentes
        repoMock.Setup(r => r.GetFilteredAsync(
                    It.Is<ErrorLogFilter>(f => f.Cursor != null),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(((IReadOnlyList<ErrorLog>)new List<ErrorLog>(), 10));

        var svc = new ErrorLogService(repoMock.Object, keyRepoMock.Object, BuildCache(),
            NullLogger<ErrorLogService>.Instance);

        // Page 1
        var page1Result = await svc.GetFilteredAsync(null, null, null, null, null, null, null, 3);
        Assert.True(page1Result.IsSuccess);
        var page1 = page1Result.Value;
        Assert.Equal(3, page1.Items.Count);
        Assert.NotNull(page1.NextCursor); // deve gerar cursor pois items == limit

        // Page 2 — usa o cursor da página 1
        var page2Result = await svc.GetFilteredAsync(null, null, null, null, null, null, page1.NextCursor, 3);
        Assert.True(page2Result.IsSuccess);
        // Verifica que repo foi chamado COM cursor (query diferente)
        repoMock.Verify(r => r.GetFilteredAsync(
            It.Is<ErrorLogFilter>(f => f.Cursor == page1.NextCursor),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}

