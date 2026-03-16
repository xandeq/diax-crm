using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ClearOldFakeModelCapability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Clear capabilities from the old fake Nano Banana model
            migrationBuilder.Sql(@"
UPDATE ""ai_models""
SET ""capabilities_json"" = NULL
WHERE model_key = 'gemini-2.5-flash-preview-image-generation';

UPDATE ""ai_models""
SET ""is_enabled"" = false
WHERE model_key = 'gemini-2.5-flash-preview-image-generation';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
