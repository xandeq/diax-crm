using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSnippetAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "snippet_attachments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    snippet_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    original_file_name = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    stored_file_name = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    content_type = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_snippet_attachments", x => x.id);
                    table.ForeignKey(
                        name: "FK_snippet_attachments_snippets_snippet_id",
                        column: x => x.snippet_id,
                        principalTable: "snippets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_snippet_attachments_snippet_id",
                table: "snippet_attachments",
                column: "snippet_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "snippet_attachments");
        }
    }
}
