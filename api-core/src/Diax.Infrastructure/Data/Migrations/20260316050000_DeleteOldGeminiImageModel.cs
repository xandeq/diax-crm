using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class DeleteOldGeminiImageModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Delete the old invalid Gemini image generation model
            migrationBuilder.Sql(@"
DELETE FROM ""group_ai_model_accesses""
WHERE ""ai_model_id"" IN (
    SELECT id FROM ""ai_models""
    WHERE model_key = 'gemini-2.5-flash-preview-image-generation'
);

DELETE FROM ""ai_models""
WHERE model_key = 'gemini-2.5-flash-preview-image-generation';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No rollback
        }
    }
}
