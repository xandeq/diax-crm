using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixNanoBananaInsert : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var geminiProviderId = "SELECT id FROM \"ai_providers\" WHERE key = 'gemini'";
            var adminGroupId = "SELECT id FROM \"user_groups\" WHERE key = 'system-admin'";

            // First, delete the old model
            migrationBuilder.Sql($@"
                DELETE FROM ""group_ai_model_accesses""
                WHERE ""ai_model_id"" IN (
                    SELECT id FROM ""ai_models""
                    WHERE provider_id = ({geminiProviderId})
                    AND model_key = 'gemini-2.5-flash-preview-image-generation'
                );

                DELETE FROM ""ai_models""
                WHERE provider_id = ({geminiProviderId})
                AND model_key = 'gemini-2.5-flash-preview-image-generation';
            ");

            // Insert the 3 correct image generation models one by one
            var models = new[]
            {
                ("gemini-2.5-flash-image", "Nano Banana (Gemini 2.5 Flash)"),
                ("gemini-3.1-flash-image-preview", "Nano Banana 2 (Gemini 3.1 Flash)"),
                ("gemini-3-pro-image-preview", "Nano Banana Pro (Gemini 3 Pro)")
            };

            foreach (var (modelKey, displayName) in models)
            {
                migrationBuilder.Sql($@"
                    INSERT INTO ""ai_models"" (id, provider_id, model_key, display_name, is_enabled, is_discovered, created_at)
                    SELECT
                        gen_random_uuid(),
                        ({geminiProviderId}),
                        '{modelKey}',
                        '{displayName}',
                        true,
                        false,
                        NOW()
                    WHERE NOT EXISTS (
                        SELECT 1 FROM ""ai_models""
                        WHERE provider_id = ({geminiProviderId})
                        AND model_key = '{modelKey}'
                    );
                ");
            }

            // Grant admin group access to the new models
            migrationBuilder.Sql($@"
                INSERT INTO ""group_ai_model_accesses"" (id, group_id, ai_model_id, created_at)
                SELECT
                    gen_random_uuid(),
                    ({adminGroupId}),
                    am.id,
                    NOW()
                FROM ""ai_models"" am
                WHERE am.provider_id = ({geminiProviderId})
                AND am.model_key IN (
                    'gemini-2.5-flash-image',
                    'gemini-3.1-flash-image-preview',
                    'gemini-3-pro-image-preview'
                )
                AND NOT EXISTS (
                    SELECT 1 FROM ""group_ai_model_accesses"" gama
                    WHERE gama.ai_model_id = am.id
                    AND gama.group_id = ({adminGroupId})
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No rollback
        }
    }
}
