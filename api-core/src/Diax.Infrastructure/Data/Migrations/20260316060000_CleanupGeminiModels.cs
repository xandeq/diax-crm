using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CleanupGeminiModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Disable and delete old/deprecated Gemini models
            migrationBuilder.Sql(@"
-- First, disable the old model (in case accesses reference it)
UPDATE ""ai_models""
SET ""is_enabled"" = false
WHERE model_key IN (
    'gemini-2.5-flash-preview-image-generation',
    'gemini-2.5-flash-image-generation',
    'gemini-2.0-flash-exp-image-generation'
);

-- Then delete accesses
DELETE FROM ""group_ai_model_accesses""
WHERE ""ai_model_id"" IN (
    SELECT id FROM ""ai_models""
    WHERE model_key IN (
        'gemini-2.5-flash-preview-image-generation',
        'gemini-2.5-flash-image-generation',
        'gemini-2.0-flash-exp-image-generation'
    )
);

-- Finally delete the models
DELETE FROM ""ai_models""
WHERE model_key IN (
    'gemini-2.5-flash-preview-image-generation',
    'gemini-2.5-flash-image-generation',
    'gemini-2.0-flash-exp-image-generation'
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
