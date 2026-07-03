using System;
using Diax.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Propostas comerciais com link público de aceite e PIX copia-e-cola.
    /// Migration à mão (dotnet-ef bloqueado pelo SAC) — atributos inline + snapshot manual.
    /// </summary>
    [DbContext(typeof(DiaxDbContext))]
    [Migration("20260703130000_AddProposals")]
    public partial class AddProposals : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "proposals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    customer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    public_token = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    pix_key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    valid_until = table.Column<DateTime>(type: "datetime2", nullable: true),
                    sent_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    accepted_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    paid_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    view_count = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proposals", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_proposals_public_token",
                table: "proposals",
                column: "public_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_proposals_customer_id",
                table: "proposals",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_proposals_user_id_created_at",
                table: "proposals",
                columns: new[] { "user_id", "created_at" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "proposals");
        }
    }
}
