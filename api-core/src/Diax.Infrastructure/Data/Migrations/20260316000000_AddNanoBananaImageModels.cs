using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNanoBananaImageModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove old/invalid image generation model
            migrationBuilder.Sql(@"
                DELETE FROM ""group_ai_model_accesses""
                WHERE ""ai_model_id"" IN (
                    SELECT id FROM ""ai_models""
                    WHERE provider_id = (SELECT id FROM ""ai_providers"" WHERE key = 'gemini')
                    AND model_key = 'gemini-2.5-flash-preview-image-generation'
                );

                DELETE FROM ""ai_models""
                WHERE provider_id = (SELECT id FROM ""ai_providers"" WHERE key = 'gemini')
                AND model_key = 'gemini-2.5-flash-preview-image-generation';
            ");

            // Insert the 3 correct Nano Banana image generation models
            migrationBuilder.Sql(@"
                INSERT INTO ""ai_models"" (id, provider_id, model_key, display_name, is_enabled, is_discovered, created_at)
                SELECT
                    gen_random_uuid(),
                    (SELECT id FROM ""ai_providers"" WHERE key = 'gemini'),
                    model_key,
                    display_name,
                    true,
                    false,
                    NOW()
                FROM (VALUES
                    ('gemini-2.5-flash-image', 'Nano Banana (Gemini 2.5 Flash)'),
                    ('gemini-3.1-flash-image-preview', 'Nano Banana 2 (Gemini 3.1 Flash)'),
                    ('gemini-3-pro-image-preview', 'Nano Banana Pro (Gemini 3 Pro)')
                ) AS t(model_key, display_name)
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""ai_models""
                    WHERE provider_id = (SELECT id FROM ""ai_providers"" WHERE key = 'gemini')
                    AND model_key = t.model_key
                );
            ");

            // Grant admin group access to new models
            migrationBuilder.Sql(@"
                INSERT INTO ""group_ai_model_accesses"" (id, group_id, ai_model_id, created_at)
                SELECT
                    gen_random_uuid(),
                    (SELECT id FROM ""user_groups"" WHERE key = 'system-admin'),
                    am.id,
                    NOW()
                FROM ""ai_models"" am
                WHERE am.provider_id = (SELECT id FROM ""ai_providers"" WHERE key = 'gemini')
                AND am.model_key IN (
                    'gemini-2.5-flash-image',
                    'gemini-3.1-flash-image-preview',
                    'gemini-3-pro-image-preview'
                )
                AND NOT EXISTS (
                    SELECT 1 FROM ""group_ai_model_accesses""
                    WHERE ai_model_id = am.id
                    AND group_id = (SELECT id FROM ""user_groups"" WHERE key = 'system-admin')
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No rollback needed
        }
    }
}
