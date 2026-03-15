using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOldGeminiModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove deprecated/invalid Gemini models from database
            migrationBuilder.Sql(@"
                DELETE FROM ""group_ai_model_accesses""
                WHERE ""ai_model_id"" IN (
                    SELECT am.id FROM ""ai_models"" am
                    INNER JOIN ""ai_providers"" ap ON am.provider_id = ap.id
                    WHERE ap.key = 'gemini'
                    AND am.model_key IN (
                        'imagen-3.0-generate-002',
                        'imagen-3.0-fast-generate-001',
                        'gemini-2.0-flash-exp-image-generation',
                        'gemini-2.5-flash-preview-04-17'
                    )
                );
            ");

            migrationBuilder.Sql(@"
                DELETE FROM ""ai_models""
                WHERE provider_id = (SELECT id FROM ""ai_providers"" WHERE key = 'gemini')
                AND model_key IN (
                    'imagen-3.0-generate-002',
                    'imagen-3.0-fast-generate-001',
                    'gemini-2.0-flash-exp-image-generation',
                    'gemini-2.5-flash-preview-04-17'
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No rollback needed — deprecated models should not be restored
        }
    }
}
