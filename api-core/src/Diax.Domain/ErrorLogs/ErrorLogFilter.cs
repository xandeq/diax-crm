namespace Diax.Domain.ErrorLogs;

public record ErrorLogFilter(
    string? AppName,
    ErrorLogLevel? Level,
    bool? IsResolved,
    DateTime? FromDate,
    DateTime? ToDate,
    string? Search,
    string? Cursor,
    int Limit = 50);
