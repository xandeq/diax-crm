using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAppLogsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_incomes_income_categories_income_category_id",
                table: "incomes");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "incomes",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.CreateTable(
                name: "app_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    timestamp_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    level = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    message = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    message_template = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    source = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    request_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    correlation_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    user_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    user_name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    request_path = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    query_string = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    http_method = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    status_code = table.Column<int>(type: "int", nullable: true),
                    headers_json = table.Column<string>(type: "nvarchar(max)", maxLength: 256, nullable: true),
                    client_ip = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    user_agent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    exception_type = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    exception_message = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    stack_trace = table.Column<string>(type: "nvarchar(max)", maxLength: 256, nullable: true),
                    inner_exception = table.Column<string>(type: "nvarchar(max)", maxLength: 256, nullable: true),
                    target_site = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    machine_name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    environment = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    additional_data = table.Column<string>(type: "nvarchar(max)", maxLength: 256, nullable: true),
                    response_time_ms = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_logs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_app_logs_correlation_id",
                table: "app_logs",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "IX_app_logs_level_timestamp",
                table: "app_logs",
                columns: new[] { "level", "timestamp_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_app_logs_request_id",
                table: "app_logs",
                column: "request_id");

            migrationBuilder.CreateIndex(
                name: "IX_app_logs_timestamp_utc",
                table: "app_logs",
                column: "timestamp_utc",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_app_logs_user_id",
                table: "app_logs",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_incomes_income_categories_income_category_id",
                table: "incomes",
                column: "income_category_id",
                principalTable: "income_categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_incomes_income_categories_income_category_id",
                table: "incomes");

            migrationBuilder.DropTable(
                name: "app_logs");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "incomes",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AddForeignKey(
                name: "FK_incomes_income_categories_income_category_id",
                table: "incomes",
                column: "income_category_id",
                principalTable: "income_categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
