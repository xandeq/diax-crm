using Diax.Domain.Logs;

namespace Diax.Application.Logs.Dtos;

public record AppLogResponse(
    Guid Id,
    DateTime TimestampUtc,
    LogLevel Level,
    LogCategory Category,
    string Message,
    string? MessageTemplate,
    string? Source,
    string? RequestId,
    string? CorrelationId,
    string? UserId,
    string? UserName,
    string? RequestPath,
    string? QueryString,
    string? HttpMethod,
    int? StatusCode,
    string? HeadersJson,
    string? ClientIp,
    string? UserAgent,
    string? ExceptionType,
    string? ExceptionMessage,
    string? StackTrace,
    string? InnerException,
    string? TargetSite,
    string? MachineName,
    string? Environment,
    string? AdditionalData,
    long? ResponseTimeMs);

public record AppLogListItemResponse(
    Guid Id,
    DateTime TimestampUtc,
    LogLevel Level,
    LogCategory Category,
    string Message,
    string? RequestPath,
    string? UserId,
    string? CorrelationId,
    string? RequestId,
    string? ExceptionType);

public record AppLogFilterRequest(
    DateTime? FromDate,
    DateTime? ToDate,
    LogLevel? Level,
    LogCategory? Category,
    string? Search,
    string? UserId,
    string? CorrelationId,
    string? RequestId,
    string? Path,
    int Page = 1,
    int PageSize = 50);

public record AppLogStatsResponse(
    int TotalCount,
    int DebugCount,
    int InformationCount,
    int WarningCount,
    int ErrorCount,
    int CriticalCount);

public record AppLogPagedResponse(
    IReadOnlyList<AppLogListItemResponse> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
