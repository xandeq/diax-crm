using Diax.Domain.Audit;
using Diax.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Data.Interceptors;

/// <summary>
/// Interceptor EF Core que captura automaticamente mudanças de entidades
/// e cria entradas de AuditLog na mesma transação.
///
/// Garantias:
/// - Nunca propaga exceção (falha silenciosa com log)
/// - Nunca cria log de AuditLogEntry (evita loop infinito)
/// - Nunca cria log de Update vazio (sem propriedades relevantes alteradas)
/// </summary>
public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService? _currentUserService;
    private readonly ILogger<AuditSaveChangesInterceptor> _logger;

    public AuditSaveChangesInterceptor(
        ICurrentUserService? currentUserService,
        ILogger<AuditSaveChangesInterceptor> logger)
    {
        _currentUserService = currentUserService;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            ProcessEntries(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
            ProcessEntries(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    // ===== Lógica central =====

    private void ProcessEntries(DbContext context)
    {
        var userId = _currentUserService?.UserId;

        // Materializa a lista antes do loop para não iterar novos DbSet<AuditLogEntry>
        var entries = context.ChangeTracker
            .Entries()
            .Where(e =>
                e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted
                && e.Entity is not AuditLogEntry) // evita loop infinito
            .ToList();

        foreach (var entry in entries)
        {
            try
            {
                var auditEntry = BuildAuditEntry(entry, userId);
                if (auditEntry is not null)
                    context.Set<AuditLogEntry>().Add(auditEntry);
            }
            catch (Exception ex)
            {
                // Falha silenciosa: o log não deve quebrar a transação principal
                _logger.LogError(ex,
                    "AuditSaveChangesInterceptor: falha ao auditar {EntityType} ({State})",
                    entry.Entity.GetType().Name,
                    entry.State);
            }
        }
    }

    private static AuditLogEntry? BuildAuditEntry(
        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry,
        Guid? userId)
    {
        var entityType = entry.Entity.GetType().Name;
        var entityId = AuditChangeCapture.GetEntityId(entry);

        // Sem ID rastreável → ignora (entidades sem PK não fazem sentido no log)
        if (string.IsNullOrEmpty(entityId))
            return null;

        var action = entry.State switch
        {
            EntityState.Added => AuditAction.Create,
            EntityState.Modified => AuditAction.Update,
            EntityState.Deleted => AuditAction.Delete,
            _ => (AuditAction?)null
        };

        if (action is null)
            return null;

        var changedProps = AuditChangeCapture.GetChangedProperties(entry);

        // Para Update: ignora se nenhuma propriedade relevante foi alterada
        if (action == AuditAction.Update && string.IsNullOrEmpty(changedProps))
            return null;

        var oldValues = AuditChangeCapture.SerializeOldValues(entry);
        var newValues = AuditChangeCapture.SerializeNewValues(entry);
        var description = BuildDescription(entityType, action.Value, entityId);

        return AuditLogEntry.Create(
            userId: userId,
            action: action.Value,
            resourceType: entityType,
            resourceId: entityId,
            description: description,
            oldValues: oldValues,
            newValues: newValues,
            changedProperties: changedProps,
            source: AuditSource.Api);
    }

    private static string BuildDescription(string entityType, AuditAction action, string entityId)
    {
        return action switch
        {
            AuditAction.Create => $"Criado: {entityType} [{entityId}]",
            AuditAction.Update => $"Atualizado: {entityType} [{entityId}]",
            AuditAction.Delete => $"Deletado: {entityType} [{entityId}]",
            _ => $"{action}: {entityType} [{entityId}]"
        };
    }
}
