using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixNanoBananaPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Delete old invalid Gemini image models
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

            // Insert correct Nano Banana models using proper PostgreSQL DO block syntax
            migrationBuilder.Sql(@"
DO $$
DECLARE
    v_gemini_provider_id uuid;
    v_admin_group_id uuid;
BEGIN
    -- Get Gemini provider ID
    SELECT id INTO v_gemini_provider_id FROM ""ai_providers"" WHERE key = 'gemini' LIMIT 1;

    -- Get admin group ID
    SELECT id INTO v_admin_group_id FROM ""user_groups"" WHERE key = 'system-admin' LIMIT 1;

    IF v_gemini_provider_id IS NOT NULL THEN
        -- Insert Nano Banana models
        INSERT INTO ""ai_models"" (id, provider_id, model_key, display_name, is_enabled, is_discovered, created_at)
        VALUES
            (gen_random_uuid(), v_gemini_provider_id, 'gemini-2.5-flash-image', 'Nano Banana (Gemini 2.5 Flash)', true, false, NOW()),
            (gen_random_uuid(), v_gemini_provider_id, 'gemini-3.1-flash-image-preview', 'Nano Banana 2 (Gemini 3.1 Flash)', true, false, NOW()),
            (gen_random_uuid(), v_gemini_provider_id, 'gemini-3-pro-image-preview', 'Nano Banana Pro (Gemini 3 Pro)', true, false, NOW())
        ON CONFLICT DO NOTHING;

        -- Grant admin group access to new models
        IF v_admin_group_id IS NOT NULL THEN
            INSERT INTO ""group_ai_model_accesses"" (id, group_id, ai_model_id, created_at)
            SELECT
                gen_random_uuid(),
                v_admin_group_id,
                am.id,
                NOW()
            FROM ""ai_models"" am
            WHERE am.model_key IN (
                'gemini-2.5-flash-image',
                'gemini-3.1-flash-image-preview',
                'gemini-3-pro-image-preview'
            )
            ON CONFLICT DO NOTHING;
        END IF;
    END IF;
END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No rollback
        }
    }
}
