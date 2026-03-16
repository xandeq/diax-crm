using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class DisableOldGeminiModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Disable the old model so it won't show up in the catalog
            migrationBuilder.Sql("UPDATE \"ai_models\" SET \"is_enabled\" = false WHERE model_key = 'gemini-2.5-flash-preview-image-generation';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"ai_models\" SET \"is_enabled\" = true WHERE model_key = 'gemini-2.5-flash-preview-image-generation';");
        }
    }
}
