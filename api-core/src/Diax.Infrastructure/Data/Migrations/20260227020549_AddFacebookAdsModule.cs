using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFacebookAdsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "facebook_ad_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ad_account_id = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    access_token = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    account_name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    currency = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    timezone = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    account_status = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    last_sync_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_facebook_ad_accounts", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_facebook_ad_accounts_user_id",
                table: "facebook_ad_accounts",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "facebook_ad_accounts");
        }
    }
}
