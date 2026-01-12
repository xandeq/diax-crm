using Diax.Domain.Common;

namespace Diax.Domain.Logs;

/// <summary>
/// Entidade que representa um registro de log no sistema.
/// Armazena informações detalhadas sobre eventos, erros e exceções.
/// </summary>
public class AppLog : Entity
{
    /// <summary>
    /// Data e hora UTC do registro.
    /// </summary>
    public DateTime TimestampUtc { get; private set; }

    /// <summary>
    /// Nível do log (Debug, Information, Warning, Error, Critical).
    /// </summary>
    public LogLevel Level { get; private set; }

    /// <summary>
    /// Categoria do log para classificação.
    /// </summary>
    public LogCategory Category { get; private set; }

    /// <summary>
    /// Mensagem principal do log.
    /// </summary>
    public string Message { get; private set; } = string.Empty;

    /// <summary>
    /// Template da mensagem (para logs estruturados).
    /// </summary>
    public string? MessageTemplate { get; private set; }

    /// <summary>
    /// Fonte do log (controller, service, etc.).
    /// </summary>
    public string? Source { get; private set; }

    // ===== Contexto HTTP =====

    /// <summary>
    /// ID único da requisição HTTP.
    /// </summary>
    public string? RequestId { get; private set; }

    /// <summary>
    /// ID de correlação para rastrear fluxos entre serviços.
    /// </summary>
    public string? CorrelationId { get; private set; }

    /// <summary>
    /// ID do usuário que realizou a ação (se autenticado).
    /// </summary>
    public string? UserId { get; private set; }

    /// <summary>
    /// Nome do usuário (se disponível).
    /// </summary>
    public string? UserName { get; private set; }

    /// <summary>
    /// Caminho da requisição HTTP (ex: /api/v1/customers).
    /// </summary>
    public string? RequestPath { get; private set; }

    /// <summary>
    /// Query string da requisição.
    /// </summary>
    public string? QueryString { get; private set; }

    /// <summary>
    /// Método HTTP (GET, POST, PUT, DELETE, etc.).
    /// </summary>
    public string? HttpMethod { get; private set; }

    /// <summary>
    /// Código de status HTTP da resposta.
    /// </summary>
    public int? StatusCode { get; private set; }

    /// <summary>
    /// Headers da requisição em formato JSON (filtrados para segurança).
    /// </summary>
    public string? HeadersJson { get; private set; }

    /// <summary>
    /// IP do cliente.
    /// </summary>
    public string? ClientIp { get; private set; }

    /// <summary>
    /// User Agent do cliente.
    /// </summary>
    public string? UserAgent { get; private set; }

    // ===== Exceção =====

    /// <summary>
    /// Tipo da exceção (ex: NullReferenceException).
    /// </summary>
    public string? ExceptionType { get; private set; }

    /// <summary>
    /// Mensagem da exceção.
    /// </summary>
    public string? ExceptionMessage { get; private set; }

    /// <summary>
    /// Stack trace completo.
    /// </summary>
    public string? StackTrace { get; private set; }

    /// <summary>
    /// Inner exception (se houver).
    /// </summary>
    public string? InnerException { get; private set; }

    /// <summary>
    /// Método onde a exceção ocorreu.
    /// </summary>
    public string? TargetSite { get; private set; }

    // ===== Ambiente =====

    /// <summary>
    /// Nome da máquina/servidor.
    /// </summary>
    public string? MachineName { get; private set; }

    /// <summary>
    /// Nome do ambiente (Development, Production, etc.).
    /// </summary>
    public string? Environment { get; private set; }

    /// <summary>
    /// Dados adicionais em formato JSON.
    /// </summary>
    public string? AdditionalData { get; private set; }

    /// <summary>
    /// Tempo de resposta em milissegundos.
    /// </summary>
    public long? ResponseTimeMs { get; private set; }

    // ===== Construtores =====

    /// <summary>
    /// Construtor protegido para EF Core.
    /// </summary>
    protected AppLog() { }

    /// <summary>
    /// Cria um novo registro de log.
    /// </summary>
    public AppLog(
        LogLevel level,
        LogCategory category,
        string message,
        string? source = null,
        string? requestId = null,
        string? correlationId = null,
        string? userId = null,
        string? userName = null,
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
        string? innerException = null,
        string? targetSite = null,
        string? machineName = null,
        string? environment = null,
        string? additionalData = null,
        long? responseTimeMs = null,
        string? messageTemplate = null)
    {
        TimestampUtc = DateTime.UtcNow;
        Level = level;
        Category = category;
        Message = message ?? throw new ArgumentNullException(nameof(message));
        MessageTemplate = messageTemplate;
        Source = source;
        RequestId = requestId;
        CorrelationId = correlationId;
        UserId = userId;
        UserName = userName;
        RequestPath = requestPath;
        QueryString = queryString;
        HttpMethod = httpMethod;
        StatusCode = statusCode;
        HeadersJson = headersJson;
        ClientIp = clientIp;
        UserAgent = userAgent;
        ExceptionType = exceptionType;
        ExceptionMessage = exceptionMessage;
        StackTrace = stackTrace;
        InnerException = innerException;
        TargetSite = targetSite;
        MachineName = machineName;
        Environment = environment;
        AdditionalData = additionalData;
        ResponseTimeMs = responseTimeMs;
    }

    /// <summary>
    /// Cria um log de erro a partir de uma exceção.
    /// </summary>
    public static AppLog FromException(
        Exception exception,
        string? requestId = null,
        string? correlationId = null,
        string? userId = null,
        string? requestPath = null,
        string? httpMethod = null,
        string? clientIp = null,
        string? userAgent = null,
        string? headersJson = null,
        string? queryString = null,
        string? environment = null)
    {
        var innerExceptions = new List<string>();
        var innerEx = exception.InnerException;
        while (innerEx != null)
        {
            innerExceptions.Add($"{innerEx.GetType().Name}: {innerEx.Message}");
            innerEx = innerEx.InnerException;
        }

        return new AppLog(
            level: LogLevel.Error,
            category: LogCategory.System,
            message: exception.Message,
            source: exception.Source,
            requestId: requestId,
            correlationId: correlationId,
            userId: userId,
            requestPath: requestPath,
            httpMethod: httpMethod,
            clientIp: clientIp,
            userAgent: userAgent,
            headersJson: headersJson,
            queryString: queryString,
            exceptionType: exception.GetType().FullName,
            exceptionMessage: exception.Message,
            stackTrace: exception.StackTrace,
            innerException: innerExceptions.Count > 0 ? string.Join(" -> ", innerExceptions) : null,
            targetSite: exception.TargetSite?.ToString(),
            machineName: System.Environment.MachineName,
            environment: environment);
    }

    /// <summary>
    /// Cria um log de requisição HTTP (geralmente para erros 4xx/5xx).
    /// </summary>
    public static AppLog Create(
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
        int? responseTimeMs = null)
    {
        return new AppLog(
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
            responseTimeMs: responseTimeMs,
            machineName: System.Environment.MachineName,
            environment: System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
    }
}
