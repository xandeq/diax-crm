using System.Text.Json;
using Diax.Application.EmailMarketing;
using Diax.Application.SurfAlerts.Dtos;
using Diax.Domain.ApiKeys;
using Diax.Domain.SurfAlerts;
using Diax.Shared.Results;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Diax.Application.SurfAlerts;

public class SurfAlertService : ISurfAlertService
{
    private const string IngestScope = "surf-alerts.ingest";
    private const string ViewScope = "surf-alerts.view";
    private const int RateLimitPerHour = 30;
    private const int MaxRecentLimit = 100;

    private readonly ISurfAlertRepository _repo;
    private readonly IApiKeyRepository _apiKeyRepo;
    private readonly IEmailOpsAlerter _alerter;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SurfAlertService> _logger;

    public SurfAlertService(
        ISurfAlertRepository repo,
        IApiKeyRepository apiKeyRepo,
        IEmailOpsAlerter alerter,
        IMemoryCache cache,
        ILogger<SurfAlertService> logger)
    {
        _repo = repo;
        _apiKeyRepo = apiKeyRepo;
        _alerter = alerter;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result<IngestSurfAlertResponse>> IngestAsync(
        string apiKeyPlain, string? idempotencyKey, IngestSurfAlertRequest request, CancellationToken ct = default)
    {
        var authResult = await ValidateApiKeyAsync(apiKeyPlain, IngestScope, ct);
        if (!authResult.IsSuccess) return Result.Failure<IngestSurfAlertResponse>(authResult.Error);

        var keyHash = ApiKey.HashKey(apiKeyPlain);
        var rateLimitResult = CheckRateLimit(keyHash);
        if (!rateLimitResult.IsSuccess) return Result.Failure<IngestSurfAlertResponse>(rateLimitResult.Error);

        if (request.Weekend.Sunday < request.Weekend.Saturday)
            return Result.Failure<IngestSurfAlertResponse>(
                Error.Validation("SurfAlert.InvalidWeekend", "weekend.sunday não pode ser anterior a weekend.saturday."));

        // Idempotência: mesma chave já processada → devolve o registro existente, sem duplicar.
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existing = await _repo.GetByIdempotencyKeyAsync(idempotencyKey, ct);
            if (existing is not null)
                return Result.Success(new IngestSurfAlertResponse { Id = existing.Id, Status = "duplicate" });
        }

        var alert = SurfAlert.Create(
            request.Source,
            request.Type,
            request.GeneratedAt.UtcDateTime,
            request.Weekend.Saturday,
            request.Weekend.Sunday,
            request.HasGoodWindow,
            request.Summary,
            request.BestWindow is null ? null : JsonSerializer.Serialize(request.BestWindow),
            request.Alternatives.Count == 0 ? null : JsonSerializer.Serialize(request.Alternatives),
            request.CalendarConflicts.Count == 0 ? null : JsonSerializer.Serialize(request.CalendarConflicts),
            request.Sources.Count == 0 ? null : JsonSerializer.Serialize(request.Sources),
            JsonSerializer.Serialize(request),
            idempotencyKey);

        try
        {
            await _repo.AddAsync(alert, ct);
            await _repo.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (IsUniqueConstraintViolation(ex))
        {
            // Race condition: outra requisição com a mesma Idempotency-Key venceu a corrida.
            var existing = !string.IsNullOrWhiteSpace(idempotencyKey)
                ? await _repo.GetByIdempotencyKeyAsync(idempotencyKey, ct)
                : null;
            if (existing is not null)
                return Result.Success(new IngestSurfAlertResponse { Id = existing.Id, Status = "duplicate" });
            throw;
        }

        if (request.HasGoodWindow)
        {
            // Nunca derruba o ingest por falha de notificação — best-effort, com cooldown por fim de semana.
            _ = _alerter.NotifyAsync(
                $"surf-alert:{request.Weekend.Saturday:yyyy-MM-dd}",
                $"🏄 <b>Janela boa de surf</b><br>{System.Net.WebUtility.HtmlEncode(request.Summary)}",
                ct);
        }

        return Result.Success(new IngestSurfAlertResponse { Id = alert.Id, Status = "created" });
    }

    public async Task<Result<IReadOnlyList<SurfAlertResponse>>> GetRecentAsync(
        string? apiKeyPlain, int limit, CancellationToken ct = default)
    {
        if (apiKeyPlain is not null)
        {
            var authResult = await ValidateApiKeyAsync(apiKeyPlain, ViewScope, ct);
            if (!authResult.IsSuccess) return Result.Failure<IReadOnlyList<SurfAlertResponse>>(authResult.Error);
        }

        var items = await _repo.GetRecentAsync(Math.Clamp(limit, 1, MaxRecentLimit), ct);
        return Result.Success<IReadOnlyList<SurfAlertResponse>>(
            items.Select(SurfAlertResponse.FromEntity).ToList());
    }

    private async Task<Result<ApiKey>> ValidateApiKeyAsync(string apiKeyPlain, string requiredScope, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(apiKeyPlain))
            return Result.Failure<ApiKey>(new Error("SurfAlert.Unauthorized", "API Key ausente."));

        var keyHash = ApiKey.HashKey(apiKeyPlain);
        var apiKey = await _apiKeyRepo.GetByKeyHashAsync(keyHash, ct);

        if (apiKey is null || !apiKey.IsEnabled)
        {
            _logger.LogWarning("Acesso a surf alert recusado: API Key inválida ou desabilitada (hash prefix: {P})", keyHash[..8]);
            return Result.Failure<ApiKey>(new Error("SurfAlert.Unauthorized", "API Key inválida ou desabilitada."));
        }

        if (apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt.Value < DateTime.UtcNow)
            return Result.Failure<ApiKey>(new Error("SurfAlert.Unauthorized", "API Key expirada."));

        var scopes = apiKey.Scope.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (!scopes.Any(s => s.Equals(requiredScope, StringComparison.OrdinalIgnoreCase)))
            return Result.Failure<ApiKey>(new Error("SurfAlert.Forbidden", $"API Key não tem escopo '{requiredScope}'."));

        _ = _apiKeyRepo.RecordUsageAsync(apiKey.Id, ct);

        return Result.Success(apiKey);
    }

    private Result CheckRateLimit(string keyHash)
    {
        var cacheKey = $"rl:surfalert:{keyHash}";
        var bucket = _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return new RateBucket();
        })!;

        var after = bucket.Add(1);
        if (after > RateLimitPerHour)
        {
            bucket.Add(-1);
            return Result.Failure(new Error("SurfAlert.RateLimited",
                $"Limite de {RateLimitPerHour} requisições/hora excedido. Tente novamente em instantes."));
        }

        return Result.Success();
    }

    private static bool IsUniqueConstraintViolation(Exception ex)
    {
        var inner = ex.InnerException?.Message ?? ex.Message;
        return inner.Contains("2601") || inner.Contains("2627") ||
               inner.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) ||
               inner.Contains("duplicate key", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class RateBucket
    {
        private int _count;
        public int Add(int delta) => Interlocked.Add(ref _count, delta);
    }
}
