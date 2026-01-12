using Diax.Application.Logs.Dtos;
using Diax.Domain.Logs;
using Diax.Shared.Results;

namespace Diax.Application.Logs;

public interface IAppLogService
{
    Task<Result<AppLogPagedResponse>> GetFilteredAsync(
        AppLogFilterRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<AppLogResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result<AppLogStatsResponse>> GetStatsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    Task<Result<Guid>> CreateAsync(
        AppLog log,
        CancellationToken cancellationToken = default);

    Task<Result<int>> CleanupAsync(
        int olderThanDays,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registra um log de requisição HTTP (especialmente para erros 4xx/5xx).
    /// </summary>
    Task<Result<Guid>> LogAsync(
        LogLevel level,
        LogCategory category,
        string message,
        string? correlationId = null,
        string? userId = null,
        string? requestPath = null,
        string? queryString = null,
        string? httpMethod = null,
        int? statusCode = null,
        string? headersJson = null,
        string? clientIp = null,
        string? userAgent = null,
        string? exceptionType = null,
        string? exceptionMessage = null,
        string? stackTrace = null,
        string? additionalData = null,
        int? responseTimeMs = null,
        CancellationToken cancellationToken = default);
}
