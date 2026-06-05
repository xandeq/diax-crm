using Diax.Domain.ErrorLogs;

namespace Diax.Application.ErrorLogs.Dtos;

public class ErrorLogResponse
{
    public Guid Id { get; set; }
    public string AppName { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ExceptionType { get; set; }
    public string? StackTrace { get; set; }
    public string? Source { get; set; }
    public int? LineNumber { get; set; }
    public string? RequestMethod { get; set; }
    public string? RequestPath { get; set; }
    public string? UserId { get; set; }
    public string? AdditionalData { get; set; }
    public string? Fingerprint { get; set; }
    public int OccurrenceCount { get; set; }
    public DateTime OccurredAt { get; set; }
    public DateTime FirstSeenAt { get; set; }
    public DateTime LastSeenAt { get; set; }
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNote { get; set; }

    public static ErrorLogResponse FromEntity(ErrorLog e) => new()
    {
        Id = e.Id,
        AppName = e.AppName,
        Environment = e.Environment,
        Level = e.Level.ToString(),
        Message = e.Message,
        ExceptionType = e.ExceptionType,
        StackTrace = e.StackTrace,
        Source = e.Source,
        LineNumber = e.LineNumber,
        RequestMethod = e.RequestMethod,
        RequestPath = e.RequestPath,
        UserId = e.UserId,
        AdditionalData = e.AdditionalData,
        Fingerprint = e.Fingerprint,
        OccurrenceCount = e.OccurrenceCount,
        OccurredAt = e.OccurredAt,
        FirstSeenAt = e.FirstSeenAt,
        LastSeenAt = e.LastSeenAt,
        IsResolved = e.IsResolved,
        ResolvedAt = e.ResolvedAt,
        ResolutionNote = e.ResolutionNote
    };
}

public class ErrorLogPagedResponse
{
    public IReadOnlyList<ErrorLogResponse> Items { get; set; } = [];
    public int TotalCount { get; set; }
    /// <summary>Cursor para a próxima página. Null = não há mais páginas.</summary>
    public string? NextCursor { get; set; }
}

public class ErrorLogStatsResponse
{
    public int TotalToday { get; set; }
    public int CriticalToday { get; set; }
    public int UnresolvedTotal { get; set; }
    public IReadOnlyList<AppErrorCountResponse> ByApp { get; set; } = [];
}

public class AppErrorCountResponse
{
    public string AppName { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class ResolveErrorLogRequest
{
    public string? Note { get; set; }
}

public class IngestResponse
{
    public Guid Id { get; set; }
    public string? Fingerprint { get; set; }
    public bool IsDuplicate { get; set; }
}
