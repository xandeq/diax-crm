using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    public partial class RenameAdminUsersToUsers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Create 'users' table (matching User.cs)
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            // 2. Data Migration: admin_users -> users
            migrationBuilder.Sql(@"
                INSERT INTO users (id, email, password_hash, is_active, created_at, updated_at)
                SELECT id, email, password_hash, 1, created_at, updated_at
                FROM admin_users
            ");

            // 3. Update 'user_group_members' FK
            // Existing FK: FK_user_group_members_admin_users_user_id
            migrationBuilder.DropForeignKey(
                name: "FK_user_group_members_admin_users_user_id",
                table: "user_group_members");

            // Add new FK to users
            migrationBuilder.AddForeignKey(
                name: "FK_user_group_members_users_user_id",
                table: "user_group_members",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            // 4. Drop 'admin_users'
            migrationBuilder.DropTable(
                name: "admin_users");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert!
            migrationBuilder.CreateTable(
                name: "admin_users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admin_users", x => x.id);
                });

            migrationBuilder.Sql(@"
                INSERT INTO admin_users (id, email, password_hash, created_at, updated_at, name)
                SELECT id, email, password_hash, created_at, updated_at, email
                FROM users
            ");

            migrationBuilder.DropForeignKey(
                name: "FK_user_group_members_users_user_id",
                table: "user_group_members");

            migrationBuilder.AddForeignKey(
                name: "FK_user_group_members_admin_users_user_id",
                table: "user_group_members",
                column: "user_id",
                principalTable: "admin_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
