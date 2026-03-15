using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CleanupInvalidGeminiModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Delete all invalid/deprecated Gemini models from ALL providers
            migrationBuilder.Sql(@"
                DELETE FROM ""group_ai_model_accesses""
                WHERE ""ai_model_id"" IN (
                    SELECT id FROM ""ai_models""
                    WHERE model_key IN (
                        -- Gemini provider (direct)
                        'imagen-3.0-generate-002',
                        'imagen-3.0-fast-generate-001',
                        'gemini-2.0-flash-exp-image-generation',
                        'gemini-2.5-flash-preview-04-17',
                        -- OpenRouter provider (google/prefix)
                        'google/gemini-2.0-flash-image-generation',
                        'google/gemini-2.5-flash-preview-image-generation'
                    )
                );
            ");

            migrationBuilder.Sql(@"
                DELETE FROM ""ai_models""
                WHERE model_key IN (
                    'imagen-3.0-generate-002',
                    'imagen-3.0-fast-generate-001',
                    'gemini-2.0-flash-exp-image-generation',
                    'gemini-2.5-flash-preview-04-17',
                    'google/gemini-2.0-flash-image-generation',
                    'google/gemini-2.5-flash-preview-image-generation'
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
