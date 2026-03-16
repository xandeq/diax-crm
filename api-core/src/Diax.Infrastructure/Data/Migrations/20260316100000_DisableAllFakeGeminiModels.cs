using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class DisableAllFakeGeminiModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Disable ALL fake Gemini models that don't exist in the actual Gemini API
            // These were added in error and will cause failures if users try to use them
            migrationBuilder.Sql(@"
UPDATE ""ai_models""
SET ""is_enabled"" = false
WHERE model_key IN (
    'gemini-2.5-flash-image',                        -- fake, doesn't exist
    'gemini-3.1-flash-image-preview',                -- fake, doesn't exist
    'gemini-3-pro-image-preview',                    -- fake, doesn't exist
    'gemini-2.5-flash-preview-image-generation',     -- old fake model
    'gemini-2.5-flash-image-generation',             -- fake alternative name
    'gemini-2.0-flash-exp-image-generation'          -- deprecated
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
