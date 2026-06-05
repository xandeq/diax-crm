using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Diax.Application.ErrorLogs.Dtos;
using Diax.Domain.ApiKeys;
using Diax.Domain.ErrorLogs;
using Diax.Shared.Results;
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

        var response = await ProcessSingleAsync(request, ct);
        return Result.Success(response);
    }

    // ===== Ingestão em batch =====

    public async Task<Result<IReadOnlyList<IngestResponse>>> IngestBatchAsync(
        string apiKeyPlain, BatchIngestErrorLogRequest request, CancellationToken ct = default)
    {
        var authResult = await ValidateApiKeyAsync(apiKeyPlain, ct);
        if (!authResult.IsSuccess) return Result.Failure<IReadOnlyList<IngestResponse>>(authResult.Error);

        if (request.Logs == null || request.Logs.Count == 0)
            return Result.Failure<IReadOnlyList<IngestResponse>>(
                new Error("ErrorLog.BatchEmpty", "O batch não pode ser vazio."));

        if (request.Logs.Count > MaxBatchSize)
            return Result.Failure<IReadOnlyList<IngestResponse>>(
                new Error("ErrorLog.BatchTooLarge", $"Máximo {MaxBatchSize} logs por batch."));

        var keyHash = ApiKey.HashKey(apiKeyPlain);
        var rateLimitResult = CheckRateLimit(keyHash, request.Logs.Count);
        if (!rateLimitResult.IsSuccess) return Result.Failure<IReadOnlyList<IngestResponse>>(rateLimitResult.Error);

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

        var filter = new ErrorLogFilter(appName, levelEnum, isResolved, from, to, search, cursor, Math.Min(limit, 100));
        var (items, total) = await _repo.GetFilteredAsync(filter, ct);

        return Result.Success(new ErrorLogPagedResponse
        {
            Items = items.Select(ErrorLogResponse.FromEntity).ToList(),
            TotalCount = total
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
        var log = await _repo.GetByIdAsync(id, ct);
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
        // 1. Sanitizar e scrubbing de PII
        var message = ScrubPii(TruncateUtf8(req.Message, MaxPayloadBytes));
        var stackTrace = req.StackTrace is not null
            ? ScrubPii(TruncateUtf8(req.StackTrace, MaxStackTraceBytes))
            : null;
        var additionalData = req.AdditionalData is not null
            ? ScrubPii(TruncateUtf8(JsonSerializer.Serialize(req.AdditionalData), MaxPayloadBytes))
            : null;

        // 2. Normalizar level (case-insensitive)
        if (!Enum.TryParse<ErrorLogLevel>(req.Level, true, out var level))
            level = ErrorLogLevel.Error;

        var occurredAt = req.OccurredAt?.ToUniversalTime() ?? DateTime.UtcNow;

        // 3. Fingerprint para dedupe
        var fingerprint = ErrorLog.ComputeFingerprint(req.AppName, req.ExceptionType, req.Source, req.LineNumber);

        // 4. Dedupe-on-write: se mesmo fingerprint aberto, incrementa
        if (fingerprint is not null)
        {
            var existing = await _repo.GetOpenByFingerprintAsync(fingerprint, req.AppName, ct);
            if (existing is not null)
            {
                existing.RecordOccurrence(occurredAt);
                await _repo.SaveChangesAsync(ct);
                return new IngestResponse { Id = existing.Id, Fingerprint = fingerprint, IsDuplicate = true };
            }
        }

        // 5. Criar novo log
        var log = ErrorLog.Create(
            req.AppName,
            req.Environment ?? "production",
            level,
            message,
            req.ExceptionType,
            stackTrace,
            req.Source,
            req.LineNumber,
            req.RequestMethod,
            req.RequestPath,
            req.UserId,
            additionalData,
            occurredAt);

        await _repo.AddAsync(log, ct);
        await _repo.SaveChangesAsync(ct);

        return new IngestResponse { Id = log.Id, Fingerprint = log.Fingerprint, IsDuplicate = false };
    }

    private async Task<Result> ValidateApiKeyAsync(string apiKeyPlain, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(apiKeyPlain))
            return Result.Failure(new Error("ErrorLog.Unauthorized", "API Key ausente."));

        var keyHash = ApiKey.HashKey(apiKeyPlain);
        var apiKey = await _apiKeyRepo.GetByKeyHashAsync(keyHash, ct);

        if (apiKey is null || !apiKey.IsEnabled)
            return Result.Failure(new Error("ErrorLog.Unauthorized", "API Key inválida ou desabilitada."));

        if (apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt.Value < DateTime.UtcNow)
            return Result.Failure(new Error("ErrorLog.Unauthorized", "API Key expirada."));

        if (!apiKey.Scope.Contains(RequiredScope, StringComparison.OrdinalIgnoreCase))
            return Result.Failure(new Error("ErrorLog.Forbidden", $"API Key não tem escopo '{RequiredScope}'."));

        apiKey.RecordUsage();
        return Result.Success();
    }

    private Result CheckRateLimit(string keyHash, int count = 1)
    {
        var cacheKey = $"rl:errorlog:{keyHash}";
        var bucket = _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
            return new RateBucket();
        })!;

        if (bucket.Count + count > RateLimitPerMinute)
            return Result.Failure(new Error("ErrorLog.RateLimited",
                $"Limite de {RateLimitPerMinute} requisições/minuto excedido. Tente novamente em instantes."));

        bucket.Count += count;
        return Result.Success();
    }

    // Remove CPF, e-mail, tokens Bearer, connection strings
    private static string ScrubPii(string? input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        var result = CpfPattern().Replace(input, "[CPF_REDACTED]");
        result = EmailPattern().Replace(result, "[EMAIL_REDACTED]");
        result = BearerPattern().Replace(result, "Bearer [TOKEN_REDACTED]");
        result = ConnStringPattern().Replace(result, "$1=[REDACTED]");
        return result;
    }

    private static string TruncateUtf8(string? input, int maxBytes)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        var bytes = Encoding.UTF8.GetBytes(input);
        if (bytes.Length <= maxBytes) return input;
        return Encoding.UTF8.GetString(bytes, 0, maxBytes) + "...[truncated]";
    }

    [GeneratedRegex(@"\d{3}[\.\-]?\d{3}[\.\-]?\d{3}[\-]?\d{2}", RegexOptions.Compiled)]
    private static partial Regex CpfPattern();

    [GeneratedRegex(@"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled)]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"Bearer\s+[A-Za-z0-9\-._~+/]+=*", RegexOptions.Compiled)]
    private static partial Regex BearerPattern();

    [GeneratedRegex(@"(?i)(password|pwd|passwd|secret|apikey|api_key|connectionstring)\s*=\s*[^\s;,""']+", RegexOptions.Compiled)]
    private static partial Regex ConnStringPattern();

    private sealed class RateBucket { public int Count { get; set; } }
}
