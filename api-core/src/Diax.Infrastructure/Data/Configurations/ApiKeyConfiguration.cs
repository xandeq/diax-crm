using Diax.Domain.ApiKeys;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

/// <summary>
/// Configuração EF Core para a entidade ApiKey.
/// </summary>
public class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        // ===== TABELA =====
        builder.ToTable("api_keys");

        // ===== CHAVE PRIMÁRIA =====
        builder.HasKey(x => x.Id);

        // ===== PROPRIEDADES =====
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.KeyHash)
            .IsRequired()
            .HasMaxLength(500); // SHA256 base64 = ~44 chars, mas deixamos margem

        builder.Property(x => x.IsEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.ExpiresAt);

        builder.Property(x => x.LastUsedAt);

        builder.Property(x => x.Scope)
            .IsRequired()
            .HasMaxLength(200)
            .HasDefaultValue("Blog");

        // ===== ÍNDICES =====
        // Índice único no hash da chave para busca rápida
        builder.HasIndex(x => x.KeyHash)
            .IsUnique();

        // Índice para filtrar chaves ativas
        builder.HasIndex(x => x.IsEnabled);

        // Índice para buscar por nome
        builder.HasIndex(x => x.Name);
    }
}
