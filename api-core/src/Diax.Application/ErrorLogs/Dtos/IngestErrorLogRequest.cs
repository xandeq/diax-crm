using System.ComponentModel.DataAnnotations;

namespace Diax.Application.ErrorLogs.Dtos;

public class IngestErrorLogRequest
{
    [Required, MaxLength(100)]
    public string AppName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Environment { get; set; } = "production";

    [Required, MaxLength(20)]
    public string Level { get; set; } = "Error"; // Warning | Error | Critical

    [Required]
    public string Message { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ExceptionType { get; set; }

    public string? StackTrace { get; set; }

    [MaxLength(500)]
    public string? Source { get; set; }

    public int? LineNumber { get; set; }

    [MaxLength(10)]
    public string? RequestMethod { get; set; }

    [MaxLength(1000)]
    public string? RequestPath { get; set; }

    [MaxLength(200)]
    public string? UserId { get; set; }

    public object? AdditionalData { get; set; }

    public DateTime? OccurredAt { get; set; }
}

public class BatchIngestErrorLogRequest
{
    [Required]
    public List<IngestErrorLogRequest> Logs { get; set; } = [];
}
