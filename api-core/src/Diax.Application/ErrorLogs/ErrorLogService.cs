using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Diax.Application.ErrorLogs.Dtos;
using Diax.Domain.ApiKeys;
using Diax.Domain.ErrorLogs;
using Diax.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Diax.Application.ErrorLogs;

public partial class ErrorLogService : IErrorLogService
{
    private const int MaxPayloadBytes = 32 * 1024;        // 32 KB
    private const int MaxStackTraceBytes = 8 * 1024;      // 8 KB
    private const int MaxBatchSize = 100;
    private const int RateLimitPerMinute = 50;
    private const string RequiredScope = "error-logs.ingest";

    private readonly IErrorLogRepository _repo;
    private readonly IApiKeyRepository _apiKeyRepo;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ErrorLogService> _logger;

    public ErrorLogService(
        IErrorLogRepository repo,
        IApiKeyRepository apiKeyRepo,
        IMemoryCache cache,
        ILogger<ErrorLogService> logger)
    {
        _repo = repo;
        _apiKeyRepo = apiKeyRepo;
        _cache = cache;
        _logger = logger;
    }

    // ===== Ingestão individual =====

    public async Task<Result<IngestResponse>> IngestAsync(
        string apiKeyPlain, IngestErrorLogRequest request, CancellationToken ct = default)
    {
        var authResult = await ValidateApiKeyAsync(apiKeyPlain, ct);
        if (!authResult.IsSuccess) return Result.Failure<IngestResponse>(authResult.Error);

        var keyHash = ApiKey.HashKey(apiKeyPlain);
        var rateLimitResult = CheckRateLimit(keyHash);
        if (!rateLimitResult.IsSuccess) return Result.Failure<IngestResponse>(rateLimitResult.Error);

        // A-06: AppName é derivado do Name da ApiKey — cliente não controla este campo
        // Evita cross-app log injection (key de "investiq" postando logs como "vaganagringa")
        request.AppName = authResult.Value.Name;

        var response = await ProcessSingleAsync(request, ct);
        return Result.Success(response);
    }

    // ===== Ingestão em batch =====

    public async Task<Result<IReadOnlyList<IngestResponse>>> IngestBatchAsync(
        string apiKeyPlain, BatchIngestErrorLogRequest request, CancellationToken ct = default)
    {
        // Validações baratas ANTES da query de auth (falha rápida)
        if (request.Logs == null || request.Logs.Count == 0)
            return Result.Failure<IReadOnlyList<IngestResponse>>(
                new Error("ErrorLog.BatchEmpty", "O batch não pode ser vazio."));

        if (request.Logs.Count > MaxBatchSize)
            return Result.Failure<IReadOnlyList<IngestResponse>>(
                new Error("ErrorLog.BatchTooLarge", $"Máximo {MaxBatchSize} logs por batch."));

        var authResult = await ValidateApiKeyAsync(apiKeyPlain, ct);
        if (!authResult.IsSuccess) return Result.Failure<IReadOnlyList<IngestResponse>>(authResult.Error);

        var keyHash = ApiKey.HashKey(apiKeyPlain);
        var rateLimitResult = CheckRateLimit(keyHash, request.Logs.Count);
        if (!rateLimitResult.IsSuccess) return Result.Failure<IReadOnlyList<IngestResponse>>(rateLimitResult.Error);

        // A-06: força AppName de todos os logs do batch para o nome da key autenticada
        var authorizedAppName = authResult.Value.Name;
        foreach (var l in request.Logs) l.AppName = authorizedAppName;

        var results = new List<IngestResponse>();
        foreach (var log in request.Logs)
        {
            try
            {
                var resp = await ProcessSingleAsync(log, ct);
                results.Add(resp);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao processar log do batch: {App} - {Msg}", log.AppName, log.Message);
            }
        }

        return Result.Success<IReadOnlyList<IngestResponse>>(results);
    }

    // ===== Leitura =====

    public async Task<Result<ErrorLogPagedResponse>> GetFilteredAsync(
        string? appName, string? level, bool? isResolved,
        DateTime? from, DateTime? to, string? search, string? cursor, int limit, CancellationToken ct = default)
    {
        ErrorLogLevel? levelEnum = null;
        if (!string.IsNullOrWhiteSpace(level) && Enum.TryParse<ErrorLogLevel>(level, true, out var parsed))
            levelEnum = parsed;

        var pageLimit = Math.Min(limit, 100);
        var filter = new ErrorLogFilter(appName, levelEnum, isResolved, from, to, search, cursor, pageLimit);
        var (items, total) = await _repo.GetFilteredAsync(filter, ct);

        var dtos = items.Select(ErrorLogResponse.FromEntity).ToList();

        // Gera NextCursor com base no último item retornado (keyset: OccurredAt + Id)
        string? nextCursor = null;
        if (items.Count == pageLimit && items.Count > 0)
        {
            var last = items[^1];
            nextCursor = ErrorLogFilter.EncodeCursor(last.OccurredAt, last.Id);
        }

        return Result.Success(new ErrorLogPagedResponse
        {
            Items = dtos,
            TotalCount = total,
            NextCursor = nextCursor
        });
    }

    public async Task<Result<ErrorLogResponse>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var log = await _repo.GetByIdAsync(id, ct);
        if (log is null)
            return Result.Failure<ErrorLogResponse>(Error.NotFound("ErrorLog", id));

        return Result.Success(ErrorLogResponse.FromEntity(log));
    }

    public async Task<Result<ErrorLogResponse>> ResolveAsync(Guid id, string? note, CancellationToken ct = default)
    {
        // GetByIdTrackedAsync: entidade rastreada pelo change tracker — SaveChanges persiste a mutação
        var log = await _repo.GetByIdTrackedAsync(id, ct);
        if (log is null)
            return Result.Failure<ErrorLogResponse>(Error.NotFound("ErrorLog", id));

        log.Resolve(note);
        await _repo.SaveChangesAsync(ct);

        return Result.Success(ErrorLogResponse.FromEntity(log));
    }

    public async Task<Result<ErrorLogStatsResponse>> GetStatsAsync(CancellationToken ct = default)
    {
        var stats = await _repo.GetStatsAsync(ct);
        return Result.Success(new ErrorLogStatsResponse
        {
            TotalToday = stats.TotalToday,
            CriticalToday = stats.CriticalToday,
            UnresolvedTotal = stats.UnresolvedTotal,
            ByApp = stats.ByApp.Select(x => new AppErrorCountResponse { AppName = x.AppName, Count = x.Count }).ToList()
        });
    }

    public async Task<Result<int>> CleanupAsync(int olderThanDays, CancellationToken ct = default)
    {
        if (olderThanDays < 30)
            return Result.Failure<int>(new Error("ErrorLog.CleanupMinDays", "Mínimo de 30 dias de retenção."));

        var cutoff = DateTime.UtcNow.AddDays(-olderThanDays);
        var deleted = await _repo.DeleteOlderThanAsync(cutoff, ct);
        return Result.Success(deleted);
    }

    // ===== Helpers privados =====

    private async Task<IngestResponse> ProcessSingleAsync(IngestErrorLogRequest req, CancellationToken ct)
    {
        // 1. Sanitizar e scrubbing de PII — todos os campos de texto livre
        var message       = ScrubPii(TruncateUtf8(req.Message, MaxPayloadBytes));
        var stackTrace    = req.StackTrace is not null
            ? ScrubPii(TruncateUtf8(req.StackTrace, MaxStackTraceBytes))
            : null;
        var additionalData = req.AdditionalData is not null
            ? ScrubPii(TruncateUtf8(JsonSerializer.Serialize(req.AdditionalData), MaxPayloadBytes))
            : null;
        // Campos estruturados: scrubbing leve (sem truncate pesado — já têm MaxLength no DTO)
        var requestPath   = ScrubPii(req.RequestPath);    // pode conter CPF: /api/user/096.613.297-10/
        var source        = ScrubPii(req.Source);         // caminho de arquivo pode ter username do SO
        var exceptionType = ScrubPii(req.ExceptionType);  // custom exceptions podem ter dados embutidos
        var userId        = ScrubPii(req.UserId);         // apps podem usar email como UserId

        // 2. Normalizar level (case-insensitive)
        if (!Enum.TryParse<ErrorLogLevel>(req.Level, true, out var level))
            level = ErrorLogLevel.Error;

        // Clamp OccurredAt: rejeita datas futuras (skew máx 5min) e muito antigas (>30 dias)
        // Datas inválidas são substituídas por agora — logs de backdating ou bypass de retenção não passam
        var now = DateTime.UtcNow;
        var occurredAt = req.OccurredAt?.ToUniversalTime() ?? now;
        if (occurredAt > now.AddMinutes(5))  occurredAt = now; // futuro: clamp (clock skew tolerado: 5min)
        if (occurredAt < now.AddDays(-30))   occurredAt = now; // muito antigo: clamp (>30 dias → provavelmente drift)

        // 3. Fingerprint para dedupe
        var fingerprint = ErrorLog.ComputeFingerprint(req.AppName, req.ExceptionType, req.Source, req.LineNumber);

        // 4. Dedupe atômico: tenta incrementar via UPDATE direto (sem check-then-insert)
        //    Se retornar true → já existia entrada aberta, incrementou atomicamente, sem race condition
        if (fingerprint is not null)
        {
            var incremented = await _repo.IncrementOccurrenceAtomicAsync(fingerprint, req.AppName, occurredAt, ct);
            if (incremented)
            {
                // Busca o Id atual para retornar (somente leitura, sem tracking necessário)
                var existing = await _repo.GetOpenByFingerprintAsync(fingerprint, req.AppName, ct);
                var existingId = existing?.Id ?? Guid.Empty;
                return new IngestResponse { Id = existingId, Fingerprint = fingerprint, IsDuplicate = true };
            }
        }

        // 5. Criar novo log (pode falhar com unique constraint se race condition ocorrer simultaneamente)
        var log = ErrorLog.Create(
            req.AppName,
            req.Environment ?? "production",
            level,
            message,
            exceptionType,
            stackTrace,
            source,
            req.LineNumber,
            req.RequestMethod,
            requestPath,
            userId,
            additionalData,
            occurredAt);

        try
        {
            await _repo.AddAsync(log, ct);
            await _repo.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            // Race condition residual: outro request inseriu entre nosso IncrementOccurrenceAtomicAsync e AddAsync
            // Tentativa segura: agora IncrementOccurrenceAtomicAsync vai encontrar e incrementar
            var retried = await _repo.IncrementOccurrenceAtomicAsync(fingerprint!, req.AppName, occurredAt, ct);
            var existing = retried ? await _repo.GetOpenByFingerprintAsync(fingerprint!, req.AppName, ct) : null;
            return new IngestResponse { Id = existing?.Id ?? Guid.Empty, Fingerprint = fingerprint, IsDuplicate = true };
        }

        return new IngestResponse { Id = log.Id, Fingerprint = log.Fingerprint, IsDuplicate = false };
    }

    private async Task<Result<ApiKey>> ValidateApiKeyAsync(string apiKeyPlain, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(apiKeyPlain))
            return Result.Failure<ApiKey>(new Error("ErrorLog.Unauthorized", "API Key ausente."));

        var keyHash = ApiKey.HashKey(apiKeyPlain);
        var apiKey = await _apiKeyRepo.GetByKeyHashAsync(keyHash, ct);

        if (apiKey is null || !apiKey.IsEnabled)
        {
            _logger.LogWarning("Ingest recusado: API Key inválida ou desabilitada (hash prefix: {P})", keyHash[..8]);
            return Result.Failure<ApiKey>(new Error("ErrorLog.Unauthorized", "API Key inválida ou desabilitada."));
        }

        if (apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt.Value < DateTime.UtcNow)
            return Result.Failure<ApiKey>(new Error("ErrorLog.Unauthorized", "API Key expirada."));

        // Match exato em lista (split por vírgula) — evita bypass por substring:
        // "error-logs.ingest-admin".Contains("error-logs.ingest") == true mas não deve passar
        var scopes = apiKey.Scope.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (!scopes.Any(s => s.Equals(RequiredScope, StringComparison.OrdinalIgnoreCase)))
            return Result.Failure<ApiKey>(new Error("ErrorLog.Forbidden", $"API Key não tem escopo '{RequiredScope}'."));

        // Persiste LastUsedAt via UPDATE atômico — sem carregar nem rastrear a entidade completa
        _ = _apiKeyRepo.RecordUsageAsync(apiKey.Id, ct);

        return Result.Success(apiKey);
    }

    private Result CheckRateLimit(string keyHash, int count = 1)
    {
        var cacheKey = $"rl:errorlog:{keyHash}";
        var bucket = _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
            return new RateBucket();
        })!;

        // Incremento atômico: sem race condition entre check e increment
        var after = bucket.Add(count);
        if (after > RateLimitPerMinute)
        {
            bucket.Add(-count); // desfaz — não contabiliza requisições rejeitadas
            return Result.Failure(new Error("ErrorLog.RateLimited",
                $"Limite de {RateLimitPerMinute} requisições/minuto excedido. Tente novamente em instantes."));
        }

        return Result.Success();
    }

    // Remove CPF, e-mail, tokens Bearer, connection strings
    /// <summary>Exposto como internal para testes unitários via InternalsVisibleTo.</summary>
    internal static string ScrubPiiPublic(string? input) => ScrubPii(input);

    private static string ScrubPii(string? input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        var result = CpfPattern().Replace(input, "[CPF_REDACTED]");
        result = EmailPattern().Replace(result, "[EMAIL_REDACTED]");
        result = BearerPattern().Replace(result, "Bearer [TOKEN_REDACTED]");
        result = ConnStringPattern().Replace(result, "$1=[REDACTED]");
        return result;
    }

    internal static string TruncateUtf8(string? input, int maxBytes)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        // Fast path: a maioria das strings cabe sem truncamento
        if (Encoding.UTF8.GetByteCount(input) <= maxBytes) return input;

        var bytes = Encoding.UTF8.GetBytes(input);
        // Recua até encontrar início de sequência UTF-8 válida (byte de continuação = 10xxxxxx)
        var safeLength = maxBytes;
        while (safeLength > 0 && (bytes[safeLength] & 0xC0) == 0x80)
            safeLength--;

        return Encoding.UTF8.GetString(bytes, 0, safeLength) + "...[truncated]";
    }

    [GeneratedRegex(@"\d{3}[\.\-]?\d{3}[\.\-]?\d{3}[\-]?\d{2}", RegexOptions.Compiled)]
    private static partial Regex CpfPattern();

    [GeneratedRegex(@"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled)]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"Bearer\s+[A-Za-z0-9\-._~+/]+=*", RegexOptions.Compiled)]
    private static partial Regex BearerPattern();

    [GeneratedRegex(@"(?i)(password|pwd|passwd|secret|apikey|api_key|connectionstring)\s*=\s*[^\s;,""']+", RegexOptions.Compiled)]
    private static partial Regex ConnStringPattern();

    private sealed class RateBucket
    {
        private int _count;
        /// <summary>Incremento thread-safe. Retorna o valor após a adição.</summary>
        public int Add(int delta) => Interlocked.Add(ref _count, delta);
        public int Current => Volatile.Read(ref _count);
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        // SQL Server: error number 2601 (unique index) ou 2627 (unique constraint)
        var inner = ex.InnerException?.Message ?? string.Empty;
        return inner.Contains("2601") || inner.Contains("2627") ||
               inner.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) ||
               inner.Contains("duplicate key", StringComparison.OrdinalIgnoreCase);
    }
}
