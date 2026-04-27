using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHasVariableAmount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "has_variable_amount",
                table: "transactions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "has_variable_amount",
                table: "recurring_transactions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "appointments",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "duration_minutes",
                table: "appointments",
                type: "int",
                nullable: false,
                defaultValue: 60);

            migrationBuilder.AddColumn<bool>(
                name: "is_cancelled",
                table: "appointments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "label_id",
                table: "appointments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "recurrence_group_id",
                table: "appointments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "appointment_labels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    color = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    order = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appointment_labels", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_appointments_label_id",
                table: "appointments",
                column: "label_id");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_RecurrenceGroupId",
                table: "appointments",
                column: "recurrence_group_id",
                filter: "[recurrence_group_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentLabels_UserId",
                table: "appointment_labels",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_appointments_appointment_labels_label_id",
                table: "appointments",
                column: "label_id",
                principalTable: "appointment_labels",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_appointments_appointment_labels_label_id",
                table: "appointments");

            migrationBuilder.DropTable(
                name: "appointment_labels");

            migrationBuilder.DropIndex(
                name: "IX_appointments_label_id",
                table: "appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_RecurrenceGroupId",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "has_variable_amount",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "has_variable_amount",
                table: "recurring_transactions");

            migrationBuilder.DropColumn(
                name: "duration_minutes",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "is_cancelled",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "label_id",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "recurrence_group_id",
                table: "appointments");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "appointments",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(512)",
                oldMaxLength: 512,
                oldNullable: true);
        }
    }
}
