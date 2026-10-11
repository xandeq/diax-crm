using Diax.Domain.PromptLibrary;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class PromptConfiguration : IEntityTypeConfiguration<Prompt>
{
    public void Configure(EntityTypeBuilder<Prompt> builder)
    {
        builder.ToTable("prompts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title).IsRequired().HasMaxLength(Prompt.MaxTitleLength);
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(PromptTemplate.MaxSlugLength);
        builder.Property(x => x.Description).HasMaxLength(Prompt.MaxDescriptionLength);
        builder.Property(x => x.Kind).HasConversion<byte>();
        builder.Property(x => x.Framework).HasMaxLength(Prompt.MaxFrameworkLength);
        builder.Property(x => x.TargetModel).HasMaxLength(Prompt.MaxTargetModelLength);
        builder.Property(x => x.CurrentContentHash).IsRequired().HasMaxLength(64).IsFixedLength();
        builder.Property(x => x.Preview).IsRequired().HasMaxLength(Prompt.PreviewLength);
        builder.Property(x => x.VariableNames).HasMaxLength(Prompt.MaxVariableNamesLength);
        builder.Property(x => x.Source).HasConversion<byte>();

        builder.HasMany(x => x.Versions)
            .WithOne()
            .HasForeignKey(v => v.PromptId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Versions).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.Tags)
            .WithOne()
            .HasForeignKey(t => t.PromptId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Tags).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne(x => x.Collection)
            .WithMany()
            .HasForeignKey(x => x.CollectionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.UserId, x.Slug }).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.IsArchived, x.UpdatedAt });
        builder.HasIndex(x => new { x.UserId, x.CollectionId });
        builder.HasIndex(x => new { x.UserId, x.Source, x.SourceRefId })
            .IsUnique()
            .HasFilter("[source_ref_id] IS NOT NULL");
    }
}

public class PromptVersionConfiguration : IEntityTypeConfiguration<PromptVersion>
{
    public void Configure(EntityTypeBuilder<PromptVersion> builder)
    {
        builder.ToTable("prompt_versions");
        builder.HasKey(x => x.Id);
        // O Id nasce no domínio; sem isso o EF trata a versão nova (adicionada pela navegação) como existente.
        builder.Property(x => x.Id).ValueGeneratedNever();

        // Conteúdo grande: sobrescreve o default de 256 do DbContext.
        builder.Property(x => x.Content).IsRequired().HasColumnType("nvarchar(max)").Metadata.SetMaxLength(null);
        builder.Property(x => x.VariablesJson).HasColumnType("nvarchar(max)").Metadata.SetMaxLength(null);
        builder.Property(x => x.ChangeNote).HasMaxLength(PromptVersion.MaxChangeNoteLength);
        builder.Property(x => x.ContentHash).IsRequired().HasMaxLength(64).IsFixedLength();
        builder.Property(x => x.Source).HasConversion<byte>();
        builder.Property(x => x.FrameworkApplied).HasMaxLength(40);

        builder.HasIndex(x => new { x.PromptId, x.Number }).IsUnique();
    }
}

public class PromptTagConfiguration : IEntityTypeConfiguration<PromptTag>
{
    public void Configure(EntityTypeBuilder<PromptTag> builder)
    {
        builder.ToTable("prompt_tags");
        builder.HasKey(x => new { x.PromptId, x.Name });
        builder.Property(x => x.Name).IsRequired().HasMaxLength(PromptTemplate.MaxTagLength);
        builder.HasIndex(x => x.Name);
    }
}

public class PromptCollectionConfiguration : IEntityTypeConfiguration<PromptCollection>
{
    public void Configure(EntityTypeBuilder<PromptCollection> builder)
    {
        builder.ToTable("prompt_collections");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(PromptCollection.MaxNameLength);
        builder.Property(x => x.Icon).HasMaxLength(40);
        builder.Property(x => x.Color).HasMaxLength(16);
        builder.HasIndex(x => new { x.UserId, x.Name }).IsUnique();
    }
}
