using Diax.Application.Common;
using Diax.Application.Logs.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Logs;
using Diax.Shared.Results;
using Microsoft.Extensions.Logging;
using DomainLogLevel = Diax.Domain.Logs.LogLevel;

namespace Diax.Application.Logs;

public class AppLogService : IAppLogService, IApplicationService
{
    private readonly IAppLogRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AppLogService> _logger;

    public AppLogService(
        IAppLogRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<AppLogService> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<AppLogPagedResponse>> GetFilteredAsync(
        AppLogFilterRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (items, totalCount) = await _repository.GetFilteredAsync(
                request.FromDate,
                request.ToDate,
                request.Level,
                request.Category,
                request.Search,
                request.UserId,
                request.CorrelationId,
                request.RequestId,
                request.Path,
                request.Page,
                request.PageSize,
                cancellationToken);

            var response = new AppLogPagedResponse(
                Items: items.Select(MapToListItem).ToList(),
                TotalCount: totalCount,
                Page: request.Page,
                PageSize: request.PageSize,
                TotalPages: (int)Math.Ceiling((double)totalCount / request.PageSize));

            return Result<AppLogPagedResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve logs");
            return Result.Failure<AppLogPagedResponse>(
                new Error("Logs.QueryFailed", "Failed to retrieve logs"));
        }
    }

    public async Task<Result<AppLogResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var log = await _repository.GetByIdAsync(id, cancellationToken);
            if (log == null)
            {
                return Result.Failure<AppLogResponse>(
                    new Error("Logs.NotFound", "Log entry not found"));
            }

            return Result<AppLogResponse>.Success(MapToResponse(log));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve log {LogId}", id);
            return Result.Failure<AppLogResponse>(
                new Error("Logs.QueryFailed", "Failed to retrieve log entry"));
        }
    }

    public async Task<Result<AppLogStatsResponse>> GetStatsAsync(
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stats = await _repository.GetStatsByLevelAsync(fromDate, toDate, cancellationToken);

            return Result<AppLogStatsResponse>.Success(new AppLogStatsResponse(
                TotalCount: stats.Values.Sum(),
                DebugCount: stats.GetValueOrDefault(DomainLogLevel.Debug),
                InformationCount: stats.GetValueOrDefault(DomainLogLevel.Information),
                WarningCount: stats.GetValueOrDefault(DomainLogLevel.Warning),
                ErrorCount: stats.GetValueOrDefault(DomainLogLevel.Error),
                CriticalCount: stats.GetValueOrDefault(DomainLogLevel.Critical)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve log stats");
            return Result.Failure<AppLogStatsResponse>(
                new Error("Logs.QueryFailed", "Failed to retrieve log statistics"));
        }
    }

    public async Task<Result<Guid>> CreateAsync(AppLog log, CancellationToken cancellationToken = default)
    {
        try
        {
            await _repository.AddAsync(log, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<Guid>.Success(log.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create log entry");
            return Result.Failure<Guid>(
                new Error("Logs.CreateFailed", "Failed to create log entry"));
        }
    }

    public async Task<Result<int>> CleanupAsync(int olderThanDays, CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);
            var deletedCount = await _repository.DeleteOlderThanAsync(cutoffDate, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted {Count} log entries older than {Days} days", deletedCount, olderThanDays);
            return Result<int>.Success(deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup logs");
            return Result.Failure<int>(
                new Error("Logs.CleanupFailed", "Failed to cleanup old logs"));
        }
    }

    public async Task<Result<int>> DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var deletedCount = await _repository.DeleteAllAsync(cancellationToken);
            _logger.LogInformation("Deleted all {Count} log entries", deletedCount);
            return Result<int>.Success(deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete all logs");
            return Result.Failure<int>(
                new Error("Logs.DeleteAllFailed", "Failed to delete all logs"));
        }
    }

    public async Task<Result<Guid>> LogAsync(
        DomainLogLevel level,
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
        CancellationToken cancellationToken = default)
    {
        try
        {
            var log = AppLog.Create(
                level: level,
                category: category,
                message: message,
                correlationId: correlationId,
                userId: userId,
                requestPath: requestPath,
                queryString: queryString,
                httpMethod: httpMethod,
                statusCode: statusCode,
                headersJson: headersJson,
                clientIp: clientIp,
                userAgent: userAgent,
                exceptionType: exceptionType,
                exceptionMessage: exceptionMessage,
                stackTrace: stackTrace,
                additionalData: additionalData,
                responseTimeMs: responseTimeMs);

            await _repository.AddAsync(log, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<Guid>.Success(log.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create log entry via LogAsync");
            return Result.Failure<Guid>(
                new Error("Logs.CreateFailed", "Failed to create log entry"));
        }
    }

    private static AppLogListItemResponse MapToListItem(AppLog log) => new(
        log.Id,
        log.TimestampUtc,
        log.Level,
        log.Category,
        log.Message,
        log.RequestPath,
        log.UserId,
        log.CorrelationId,
        log.RequestId,
        log.ExceptionType);

    private static AppLogResponse MapToResponse(AppLog log) => new(
        log.Id,
        log.TimestampUtc,
        log.Level,
        log.Category,
        log.Message,
        log.MessageTemplate,
        log.Source,
        log.RequestId,
        log.CorrelationId,
        log.UserId,
        log.UserName,
        log.RequestPath,
        log.QueryString,
        log.HttpMethod,
        log.StatusCode,
        log.HeadersJson,
        log.ClientIp,
        log.UserAgent,
        log.ExceptionType,
        log.ExceptionMessage,
        log.StackTrace,
        log.InnerException,
        log.TargetSite,
        log.MachineName,
        log.Environment,
        log.AdditionalData,
        log.ResponseTimeMs);
}
