using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixGeminiImageModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Delete ALL deprecated/fake Gemini image models (they don't exist in Gemini API)
            migrationBuilder.Sql(@"
DO $$
BEGIN
    -- Delete accesses for deprecated models
    DELETE FROM ""group_ai_model_accesses""
    WHERE ""ai_model_id"" IN (
        SELECT id FROM ""ai_models""
        WHERE provider_id = (SELECT id FROM ""ai_providers"" WHERE key = 'gemini')
        AND model_key IN (
            'gemini-2.5-flash-preview-image-generation',
            'gemini-2.5-flash-image',
            'gemini-3.1-flash-image-preview',
            'gemini-3-pro-image-preview',
            'gemini-2.5-flash-image-generation',
            'gemini-2.0-flash-exp-image-generation'
        )
    );

    -- Delete the deprecated models
    DELETE FROM ""ai_models""
    WHERE provider_id = (SELECT id FROM ""ai_providers"" WHERE key = 'gemini')
    AND model_key IN (
        'gemini-2.5-flash-preview-image-generation',
        'gemini-2.5-flash-image',
        'gemini-3.1-flash-image-preview',
        'gemini-3-pro-image-preview',
        'gemini-2.5-flash-image-generation',
        'gemini-2.0-flash-exp-image-generation'
    );

    -- Keep only the valid Gemini chat models that support image gen via :generateContent
    -- (Gemini API doesn't have separate image models; all chat models can generate images)
    -- Only delete if they don't already exist
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
