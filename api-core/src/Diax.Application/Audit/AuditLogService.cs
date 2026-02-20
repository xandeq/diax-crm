using Diax.Application.Audit.Dtos;
using Diax.Domain.Audit;
using Diax.Domain.Common;
using Diax.Shared.Results;
using Microsoft.Extensions.Logging;

namespace Diax.Application.Audit;

/// <summary>
/// Implementação do serviço de auditoria.
/// Lida com criação manual de logs e todas as consultas administrativas.
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(
        IAuditLogRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<AuditLogService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<Guid>> LogManualAsync(
        Guid? userId,
        AuditAction action,
        string resourceType,
        string resourceId,
        string description,
        string? oldValues = null,
        string? newValues = null,
        AuditSource source = AuditSource.Api,
        string? correlationId = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entry = AuditLogEntry.Create(
                userId, action, resourceType, resourceId, description,
                oldValues, newValues,
                changedProperties: null,
                source, correlationId, ipAddress);

            await _repository.AddAsync(entry, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogDebug(
                "Audit log manual criado: {Action} em {ResourceType}:{ResourceId} por {UserId}",
                action, resourceType, resourceId, userId);

            return Result<Guid>.Success(entry.Id);
        }
        catch (Exception ex)
        {
            // Não propaga a exceção — log manual não deve bloquear a operação principal
            _logger.LogError(ex,
                "Falha ao criar audit log manual para {Action} em {ResourceType}:{ResourceId}",
                action, resourceType, resourceId);

            return Result.Failure<Guid>(
                new Error("AuditLog.LogFailed", "Audit log não foi registrado (a operação principal não foi afetada)"));
        }
    }

    public async Task<Result<AuditLogResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var entry = await _repository.GetByIdAsync(id, cancellationToken);

            if (entry is null)
                return Result.Failure<AuditLogResponse>(
                    Error.NotFound("AuditLogEntry", id.ToString()));

            return Result<AuditLogResponse>.Success(Map(entry));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar audit log {Id}", id);
            return Result.Failure<AuditLogResponse>(
                new Error("AuditLog.GetFailed", "Erro ao recuperar log de auditoria"));
        }
    }

    public async Task<Result<AuditLogPagedResponse>> GetFilteredAsync(
        AuditLogFilterRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = new AuditLogFilter
            {
                UserId = request.UserId,
                ResourceType = request.ResourceType,
                ResourceId = request.ResourceId,
                Action = request.Action,
                Source = request.Source,
                Status = request.Status,
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                SearchText = request.SearchText,
                Page = Math.Max(1, request.Page),
                PageSize = Math.Clamp(request.PageSize, 1, 200)
            };

            var (items, totalCount) = await _repository.GetFilteredAsync(filter, cancellationToken);

            var response = new AuditLogPagedResponse(
                Items: items.Select(Map).ToList(),
                TotalCount: totalCount,
                Page: filter.Page,
                PageSize: filter.PageSize,
                TotalPages: (int)Math.Ceiling(totalCount / (double)filter.PageSize));

            return Result<AuditLogPagedResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao filtrar audit logs");
            return Result.Failure<AuditLogPagedResponse>(
                new Error("AuditLog.FilterFailed", "Erro ao listar logs de auditoria"));
        }
    }

    public async Task<Result<List<AuditLogResponse>>> GetResourceHistoryAsync(
        string resourceType,
        string resourceId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entries = await _repository.GetByResourceAsync(resourceType, resourceId, cancellationToken);
            return Result<List<AuditLogResponse>>.Success(entries.Select(Map).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar histórico de {ResourceType}:{ResourceId}", resourceType, resourceId);
            return Result.Failure<List<AuditLogResponse>>(
                new Error("AuditLog.HistoryFailed", "Erro ao recuperar histórico do recurso"));
        }
    }

    public async Task<Result<List<AuditLogResponse>>> GetUserActivityAsync(
        Guid userId,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entries = await _repository.GetByUserAsync(userId, limit, cancellationToken);
            return Result<List<AuditLogResponse>>.Success(entries.Select(Map).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar atividade do usuário {UserId}", userId);
            return Result.Failure<List<AuditLogResponse>>(
                new Error("AuditLog.ActivityFailed", "Erro ao recuperar atividade do usuário"));
        }
    }

    public async Task<Result<int>> CleanupOldLogsAsync(int olderThanDays, CancellationToken cancellationToken = default)
    {
        if (olderThanDays < 30)
            return Result.Failure<int>(
                new Error("AuditLog.InvalidRetention", "Período de retenção mínimo é 30 dias"));

        try
        {
            var cutoff = DateTime.UtcNow.AddDays(-olderThanDays);
            var deleted = await _repository.DeleteOlderThanAsync(cutoff, cancellationToken);

            _logger.LogInformation(
                "Cleanup de audit logs concluído: {DeletedCount} registros anteriores a {Cutoff} removidos",
                deleted, cutoff);

            return Result<int>.Success(deleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao executar cleanup de audit logs");
            return Result.Failure<int>(
                new Error("AuditLog.CleanupFailed", "Erro ao limpar logs antigos"));
        }
    }

    // ===== Mapper =====

    private static AuditLogResponse Map(AuditLogEntry e) => new(
        e.Id,
        e.UserId,
        e.Action.ToString(),
        e.ResourceType,
        e.ResourceId,
        e.Description,
        e.OldValues,
        e.NewValues,
        e.ChangedProperties,
        e.Source.ToString(),
        e.CorrelationId,
        e.IpAddress,
        e.TimestampUtc,
        e.Status.ToString(),
        e.ErrorMessage);
}
