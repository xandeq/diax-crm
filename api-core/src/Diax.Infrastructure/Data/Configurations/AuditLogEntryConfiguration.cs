using Diax.Domain.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

/// <summary>
/// Configuração EF Core da tabela audit_logs.
/// snake_case é aplicado automaticamente pelo DiaxDbContext.OnModelCreating,
/// portanto não é necessário declarar HasColumnName.
/// </summary>
public class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        // snake_case + tabela
        builder.ToTable("audit_logs");

        builder.HasKey(x => x.Id);

        // Campos de tamanho fixo
        builder.Property(x => x.ResourceType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.ResourceId)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.ChangedProperties)
            .HasMaxLength(1000);

        builder.Property(x => x.CorrelationId)
            .HasMaxLength(100);

        builder.Property(x => x.IpAddress)
            .HasMaxLength(64);

        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(2000);

        // Campos de texto livre (nvarchar(max))
        builder.Property(x => x.OldValues)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.NewValues)
            .HasColumnType("nvarchar(max)");

        // Enums como int
        builder.Property(x => x.Action)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.Source)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>();

        // TimestampUtc — datetime2 já aplicado globalmente
        builder.Property(x => x.TimestampUtc)
            .IsRequired();

        // ===== Índices para performance de consulta =====

        // Consultas por data (ordem desc = mais usada)
        builder.HasIndex(x => x.TimestampUtc)
            .IsDescending()
            .HasDatabaseName("IX_audit_logs_timestamp_utc");

        // Consultas por usuário
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_audit_logs_user_id");

        // Histórico de recurso específico
        builder.HasIndex(x => new { x.ResourceType, x.ResourceId })
            .HasDatabaseName("IX_audit_logs_resource");

        // Filtro por tipo de ação
        builder.HasIndex(x => x.Action)
            .HasDatabaseName("IX_audit_logs_action");

        // Correlação entre requests
        builder.HasIndex(x => x.CorrelationId)
            .HasDatabaseName("IX_audit_logs_correlation_id");

        // Atividade por usuário em ordem cronológica
        builder.HasIndex(x => new { x.UserId, x.TimestampUtc })
            .HasDatabaseName("IX_audit_logs_user_timestamp");
    }
}
