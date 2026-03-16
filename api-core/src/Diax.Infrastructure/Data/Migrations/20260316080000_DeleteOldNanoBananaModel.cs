using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class DeleteOldNanoBananaModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Delete the old "Nano Banana" model that doesn't exist in Gemini API
            migrationBuilder.Sql("DELETE FROM \"group_ai_model_accesses\" WHERE \"ai_model_id\" IN (SELECT id FROM \"ai_models\" WHERE model_key = 'gemini-2.5-flash-preview-image-generation');");
            migrationBuilder.Sql("DELETE FROM \"ai_models\" WHERE model_key = 'gemini-2.5-flash-preview-image-generation';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
