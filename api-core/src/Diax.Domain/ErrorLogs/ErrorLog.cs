using System.Security.Cryptography;
using System.Text;
using Diax.Domain.Common;

namespace Diax.Domain.ErrorLogs;

/// <summary>
/// Registra erros de aplicações externas recebidos via ingestão centralizada.
/// Suporta deduplição por Fingerprint e contagem de ocorrências.
/// </summary>
public class ErrorLog : Entity
{
    public string AppName { get; private set; } = string.Empty;
    public string Environment { get; private set; } = "production";
    public ErrorLogLevel Level { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public string? ExceptionType { get; private set; }
    public string? StackTrace { get; private set; }
    public string? Source { get; private set; }
    public int? LineNumber { get; private set; }
    public string? RequestMethod { get; private set; }
    public string? RequestPath { get; private set; }
    public string? UserId { get; private set; }
    public string? AdditionalData { get; private set; }
    public string? Fingerprint { get; private set; }
    public int OccurrenceCount { get; private set; } = 1;
    public DateTime OccurredAt { get; private set; }
    public DateTime FirstSeenAt { get; private set; }
    public DateTime LastSeenAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsResolved { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public string? ResolutionNote { get; private set; }

    protected ErrorLog() { }

    private ErrorLog(
        string appName,
        string environment,
        ErrorLogLevel level,
        string message,
        string? exceptionType,
        string? stackTrace,
        string? source,
        int? lineNumber,
        string? requestMethod,
        string? requestPath,
        string? userId,
        string? additionalData,
        DateTime occurredAt)
    {
        AppName = appName;
        Environment = environment;
        Level = level;
        Message = message;
        ExceptionType = exceptionType;
        StackTrace = stackTrace;
        Source = source;
        LineNumber = lineNumber;
        RequestMethod = requestMethod;
        RequestPath = requestPath;
        UserId = userId;
        AdditionalData = additionalData;
        OccurredAt = occurredAt;
        FirstSeenAt = occurredAt;
        LastSeenAt = occurredAt;
        CreatedAt = DateTime.UtcNow;
        IsResolved = false;
        Fingerprint = ComputeFingerprint(appName, exceptionType, source, lineNumber);
    }

    public static ErrorLog Create(
        string appName,
        string environment,
        ErrorLogLevel level,
        string message,
        string? exceptionType,
        string? stackTrace,
        string? source,
        int? lineNumber,
        string? requestMethod,
        string? requestPath,
        string? userId,
        string? additionalData,
        DateTime occurredAt)
    {
        return new ErrorLog(
            appName, environment, level, message,
            exceptionType, stackTrace, source, lineNumber,
            requestMethod, requestPath, userId, additionalData, occurredAt);
    }

    /// <summary>
    /// Incrementa o contador de ocorrências para logs com mesmo Fingerprint.
    /// Chamado pelo serviço de ingestão no padrão dedupe-on-write.
    /// </summary>
    public void RecordOccurrence(DateTime occurredAt)
    {
        OccurrenceCount++;
        LastSeenAt = occurredAt;
    }

    public void Resolve(string? note = null)
    {
        IsResolved = true;
        ResolvedAt = DateTime.UtcNow;
        ResolutionNote = note;
    }

    /// <summary>
    /// Hash SHA256 de AppName + ExceptionType + Source + LineNumber.
    /// Identifica a mesma origem de erro independente da mensagem exata.
    /// </summary>
    public static string? ComputeFingerprint(string appName, string? exceptionType, string? source, int? lineNumber)
    {
        if (string.IsNullOrWhiteSpace(exceptionType) && string.IsNullOrWhiteSpace(source))
            return null;

        var raw = $"{appName}|{exceptionType ?? ""}|{source ?? ""}|{lineNumber?.ToString() ?? ""}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash)[..16];
    }
}
