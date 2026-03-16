using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CleanAndInsertNanoBanana : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Delete old invalid model and its accesses
            migrationBuilder.Sql(@"
DELETE FROM ""group_ai_model_accesses""
WHERE ""ai_model_id"" IN (
    SELECT id FROM ""ai_models""
    WHERE model_key IN (
        'gemini-2.5-flash-preview-image-generation',
        'imagen-3.0-generate-002',
        'imagen-3.0-fast-generate-001',
        'gemini-2.0-flash-exp-image-generation',
        'gemini-2.5-flash-preview-04-17'
    )
);

DELETE FROM ""ai_models""
WHERE model_key IN (
    'gemini-2.5-flash-preview-image-generation',
    'imagen-3.0-generate-002',
    'imagen-3.0-fast-generate-001',
    'gemini-2.0-flash-exp-image-generation',
    'gemini-2.5-flash-preview-04-17'
);
            ");

            // 2. Get Gemini provider ID and insert models with proper syntax
            migrationBuilder.Sql(@"
DECLARE @GeminiProviderId UUID;
SELECT @GeminiProviderId = id FROM ""ai_providers"" WHERE key = 'gemini';

INSERT INTO ""ai_models"" (id, provider_id, model_key, display_name, is_enabled, is_discovered, created_at)
VALUES
    (gen_random_uuid(), @GeminiProviderId, 'gemini-2.5-flash-image', 'Nano Banana (Gemini 2.5 Flash)', true, false, NOW()),
    (gen_random_uuid(), @GeminiProviderId, 'gemini-3.1-flash-image-preview', 'Nano Banana 2 (Gemini 3.1 Flash)', true, false, NOW()),
    (gen_random_uuid(), @GeminiProviderId, 'gemini-3-pro-image-preview', 'Nano Banana Pro (Gemini 3 Pro)', true, false, NOW())
ON CONFLICT DO NOTHING;
            ");

            // 3. Grant admin group access to new models
            migrationBuilder.Sql(@"
INSERT INTO ""group_ai_model_accesses"" (id, group_id, ai_model_id, created_at)
SELECT
    gen_random_uuid(),
    (SELECT id FROM ""user_groups"" WHERE key = 'system-admin'),
    am.id,
    NOW()
FROM ""ai_models"" am
WHERE am.model_key IN (
    'gemini-2.5-flash-image',
    'gemini-3.1-flash-image-preview',
    'gemini-3-pro-image-preview'
)
ON CONFLICT DO NOTHING;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No rollback
        }
    }
}
