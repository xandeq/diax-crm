using Diax.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Remove os providers de vídeo órfãos criados pelo antigo VideoProviderSeeder:
    /// 'fal-ai' (duplicata de 'falai' — nenhum client responde por essa key),
    /// 'pika' e 'kling' (não existem clients; ambos funcionam via 'falai').
    /// Migration de dados apenas — nenhuma mudança de schema.
    /// Atributos inline (sem Designer) porque não há alteração de modelo;
    /// sem eles o EF ignora a migration silenciosamente.
    /// </summary>
    [DbContext(typeof(DiaxDbContext))]
    [Migration("20260702150000_RemoveOrphanVideoProviders")]
    public partial class RemoveOrphanVideoProviders : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @dead_providers TABLE (id uniqueidentifier PRIMARY KEY);
    INSERT INTO @dead_providers
        SELECT id FROM ai_providers WHERE [key] IN ('fal-ai', 'pika', 'kling');

    DECLARE @dead_models TABLE (id uniqueidentifier PRIMARY KEY);
    INSERT INTO @dead_models
        SELECT id FROM ai_models WHERE provider_id IN (SELECT id FROM @dead_providers);

    -- Filhos primeiro (FKs com Restrict)
    DELETE FROM ai_usage_logs
        WHERE provider_id IN (SELECT id FROM @dead_providers)
           OR model_id IN (SELECT id FROM @dead_models);

    DELETE FROM generated_images
        WHERE provider_id IN (SELECT id FROM @dead_providers)
           OR model_id IN (SELECT id FROM @dead_models);

    DELETE FROM group_ai_model_access
        WHERE ai_model_id IN (SELECT id FROM @dead_models);

    DELETE FROM group_ai_provider_access
        WHERE provider_id IN (SELECT id FROM @dead_providers);

    DELETE FROM ai_provider_quotas
        WHERE ai_provider_id IN (SELECT id FROM @dead_providers);

    DELETE FROM ai_provider_credentials
        WHERE provider_id IN (SELECT id FROM @dead_providers);

    DELETE FROM ai_models
        WHERE id IN (SELECT id FROM @dead_models);

    DELETE FROM ai_providers
        WHERE id IN (SELECT id FROM @dead_providers);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    -- Fallback seguro: se alguma FK não mapeada impedir o DELETE,
    -- desabilita os providers em vez de derrubar o startup (migrate roda no boot).
    UPDATE ai_providers SET is_enabled = 0 WHERE [key] IN ('fal-ai', 'pika', 'kling');
END CATCH
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Sem rollback: os providers removidos eram órfãos (sem client no código).
        }
    }
}
