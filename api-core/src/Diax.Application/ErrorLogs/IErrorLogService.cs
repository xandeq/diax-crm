using Diax.Application.ErrorLogs.Dtos;
using Diax.Shared.Results;

namespace Diax.Application.ErrorLogs;

public interface IErrorLogService
{
    Task<Result<IngestResponse>> IngestAsync(string apiKeyPlain, IngestErrorLogRequest request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<IngestResponse>>> IngestBatchAsync(string apiKeyPlain, BatchIngestErrorLogRequest request, CancellationToken ct = default);
    Task<Result<ErrorLogPagedResponse>> GetFilteredAsync(string? appName, string? level, bool? isResolved, DateTime? from, DateTime? to, string? search, string? cursor, int limit, CancellationToken ct = default);
    Task<Result<ErrorLogResponse>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<ErrorLogResponse>> ResolveAsync(Guid id, string? note, CancellationToken ct = default);
    Task<Result<ErrorLogStatsResponse>> GetStatsAsync(CancellationToken ct = default);
    Task<Result<int>> CleanupAsync(int olderThanDays, CancellationToken ct = default);
}
