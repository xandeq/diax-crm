using Diax.Domain.Customers;
using Diax.Domain.Customers.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Diax.Infrastructure.Data.Configurations;

/// <summary>
/// Configuração EF Core para a entidade Customer.
/// </summary>
public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        // ===== TABELA =====
        builder.ToTable("customers");

        // ===== CHAVE PRIMÁRIA =====
        builder.HasKey(c => c.Id);

        // ===== IDENTIDADE =====
        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.CompanyName)
            .HasMaxLength(200);

        builder.Property(c => c.PersonType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(c => c.Document)
            .HasMaxLength(14); // CNPJ tem 14 dígitos

        // ===== CONTATO =====
        builder.Property(c => c.Email)
            .HasMaxLength(255);

        builder.Property(c => c.SecondaryEmail)
            .HasMaxLength(255);

        builder.Property(c => c.Phone)
            .HasMaxLength(50); // Aumentado para suportar formatos internacionais

        builder.Property(c => c.WhatsApp)
            .HasMaxLength(50); // Aumentado para suportar formatos internacionais

        builder.Property(c => c.Website)
            .HasMaxLength(500);

        // ===== ORIGEM E CONTEXTO =====
        builder.Property(c => c.Source)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(c => c.SourceDetails)
            .HasMaxLength(500);

        builder.Property(c => c.Notes)
            .HasMaxLength(4000); // Texto longo para observações

        builder.Property(c => c.Tags)
            .HasMaxLength(500);

        // ===== STATUS E FLAGS =====
        builder.Property(c => c.Status)
            .IsRequired()
            .HasConversion<int>();

        // IsLead e IsActiveCustomer são propriedades calculadas, não mapeadas
        builder.Ignore(c => c.IsLead);
        builder.Ignore(c => c.IsActiveCustomer);

        builder.Property(c => c.ConvertedAt);

        builder.Property(c => c.LastContactAt);

        // ===== CONFIABILIDADE & SANITIZAÇÃO =====
        builder.Property(c => c.Quality)
            .HasConversion<int?>();

        builder.Property(c => c.EmailType)
            .HasConversion<int?>();

        builder.Property(c => c.HasSuspiciousDomain)
            .HasDefaultValue(false);

        builder.Property(c => c.IsEligibleForCampaigns)
            .HasDefaultValue(true);


        // ===== SEGMENTAÇÃO (Outreach) =====
        builder.Property(c => c.LeadScore);

        builder.Property(c => c.Segment)
            .HasConversion<int?>();

        builder.Property(c => c.EmailOptOut)
            .HasDefaultValue(false);

        builder.Property(c => c.LastEmailSentAt);

        builder.Property(c => c.EmailSentCount)
            .HasDefaultValue(0);

        // ===== AUDITORIA =====
        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.CreatedBy)
            .HasMaxLength(100);

        builder.Property(c => c.UpdatedAt);

        builder.Property(c => c.UpdatedBy)
            .HasMaxLength(100);

        // ===== ÍNDICES =====

        // Índice no e-mail (não único para permitir placeholders)
        // A validação de duplicatas é feita na aplicação para emails reais
        builder.HasIndex(c => c.Email)
            .HasDatabaseName("IX_Customers_Email");

        // Índice no documento (para buscas por CPF/CNPJ)
        builder.HasIndex(c => c.Document)
            .HasDatabaseName("IX_Customers_Document")
            .HasFilter("[document] IS NOT NULL"); // Índice parcial

        // Índice no status (filtros frequentes)
        builder.HasIndex(c => c.Status)
            .HasDatabaseName("IX_Customers_Status");

        // Índice na origem (relatórios por fonte)
        builder.HasIndex(c => c.Source)
            .HasDatabaseName("IX_Customers_Source");

        // Índice composto para listagens comuns
        builder.HasIndex(c => new { c.Status, c.CreatedAt })
            .HasDatabaseName("IX_Customers_Status_CreatedAt");

        // Índice para busca por nome
        builder.HasIndex(c => c.Name)
            .HasDatabaseName("IX_Customers_Name");

        // ===== WHATSAPP (Outreach) =====
        builder.Property(c => c.WhatsAppOptOut)
            .HasDefaultValue(false);

        builder.Property(c => c.LastWhatsAppSentAt);

        builder.Property(c => c.WhatsAppSentCount)
            .HasDefaultValue(0);

        // Índice para outreach: leads segmentados prontos para envio
        builder.HasIndex(c => new { c.Segment, c.Status, c.EmailOptOut })
            .HasDatabaseName("IX_Customers_Segment_Status_OptOut");

        // Índice para outreach WhatsApp: leads prontos para envio WhatsApp
        builder.HasIndex(c => new { c.Segment, c.Status, c.WhatsAppOptOut })
            .HasDatabaseName("IX_Customers_Segment_Status_WhatsAppOptOut");
    }
}
