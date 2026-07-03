using System;
using Diax.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Reuniões agendadas pelo link público (/agendar). Índice único filtrado
    /// impede double-booking. Migration à mão (dotnet-ef bloqueado pelo SAC).
    /// </summary>
    [DbContext(typeof(DiaxDbContext))]
    [Migration("20260703150000_AddMeetings")]
    public partial class AddMeetings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "meetings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    customer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    contact_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    contact_email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    contact_phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    scheduled_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    duration_minutes = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    cancelled_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meetings", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_meetings_user_slot",
                table: "meetings",
                columns: new[] { "user_id", "scheduled_at" },
                unique: true,
                filter: "[status] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_meetings_customer_id",
                table: "meetings",
                column: "customer_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "meetings");
        }
    }
}
