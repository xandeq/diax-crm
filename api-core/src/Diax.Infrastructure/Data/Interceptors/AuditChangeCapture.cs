using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Diax.Infrastructure.Data.Interceptors;

/// <summary>
/// Utilitários estáticos para capturar e serializar mudanças de entidades EF Core.
/// </summary>
internal static class AuditChangeCapture
{
    /// <summary>
    /// Propriedades que NUNCA devem constar nos logs de auditoria.
    /// Inclui dados sensíveis e campos de auto-auditoria (evita dados redundantes).
    /// </summary>
    private static readonly HashSet<string> IgnoredProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        // Auto-auditoria (já rastreada pela própria tabela de audit)
        "CreatedAt",
        "CreatedBy",
        "UpdatedAt",
        "UpdatedBy",
        // Segurança — nunca logar dados sensíveis
        "PasswordHash",
        "Password",
        "SecurityStamp",
        "ConcurrencyStamp",
        "RefreshToken",
        "ApiKeyHash",
        "SecretKey",
        "PrivateKey",
        // Chunks de bytes ou dados binários
        "RowVersion",
        "Timestamp"
    };

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Extrai o ID primário de uma entidade como string.
    /// Retorna null se não for possível determinar o ID (ex: entidade sem PK).
    /// </summary>
    public static string? GetEntityId(EntityEntry entry)
    {
        try
        {
            var key = entry.Metadata.FindPrimaryKey();
            if (key is null) return null;

            var values = key.Properties
                .Select(p =>
                {
                    var val = entry.State == EntityState.Deleted
                        ? entry.OriginalValues[p.Name]
                        : entry.CurrentValues[p.Name];
                    return val?.ToString() ?? "null";
                })
                .ToList();

            return string.Join("-", values);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Serializa os valores originais (estado anterior) de uma entidade sendo modificada ou deletada.
    /// Returns null para entidades recém-criadas ou quando os valores originais não estão disponíveis.
    /// </summary>
    public static string? SerializeOldValues(EntityEntry entry)
    {
        if (entry.State != EntityState.Modified && entry.State != EntityState.Deleted)
            return null;

        try
        {
            var dict = new Dictionary<string, object?>();

            foreach (var prop in entry.Properties)
            {
                if (ShouldIgnore(prop)) continue;

                var value = entry.OriginalValues[prop.Metadata.Name];
                dict[prop.Metadata.Name] = value == DBNull.Value ? null : value;
            }

            return dict.Count > 0 ? JsonSerializer.Serialize(dict, SerializerOptions) : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Serializa os valores atuais (estado novo) de uma entidade sendo criada ou modificada.
    /// Returns null para entidades sendo deletadas.
    /// </summary>
    public static string? SerializeNewValues(EntityEntry entry)
    {
        if (entry.State != EntityState.Added && entry.State != EntityState.Modified)
            return null;

        try
        {
            var dict = new Dictionary<string, object?>();

            foreach (var prop in entry.Properties)
            {
                if (ShouldIgnore(prop)) continue;

                var value = entry.CurrentValues[prop.Metadata.Name];
                dict[prop.Metadata.Name] = value == DBNull.Value ? null : value;
            }

            return dict.Count > 0 ? JsonSerializer.Serialize(dict, SerializerOptions) : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Retorna a lista de propriedades relevantes que foram modificadas (somente para Update).
    /// Returns null em Create/Delete ou quando nenhuma propriedade relevante foi alterada.
    /// </summary>
    public static string? GetChangedProperties(EntityEntry entry)
    {
        if (entry.State != EntityState.Modified)
            return null;

        try
        {
            var changed = entry.Properties
                .Where(p => p.IsModified && !ShouldIgnore(p))
                .Select(p => p.Metadata.Name)
                .ToList();

            return changed.Count > 0 ? string.Join(",", changed) : null;
        }
        catch
        {
            return null;
        }
    }

    private static bool ShouldIgnore(PropertyEntry prop)
    {
        // entry.Properties already returns only scalar properties (not navigations).
        // Shadow properties (used internally by EF for FK shadow keys) are excluded
        // by checking IsShadowProperty() which is available on IPropertyBase.
        return IgnoredProperties.Contains(prop.Metadata.Name)
               || prop.Metadata.IsShadowProperty();
    }
}
