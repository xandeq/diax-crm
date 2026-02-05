using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameAdminUsersToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_user_group_members_admin_users_user_id",
                table: "user_group_members");

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.Sql(@"
                INSERT INTO users (id, email, password_hash, is_active, created_at, created_by, updated_at, updated_by)
                SELECT id, email, password_hash, is_active, created_at, created_by, updated_at, updated_by
                FROM admin_users
            ");

            // Seed System Admin Group and Assign Roles
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM user_groups WHERE [key] = 'system-admin')
                BEGIN
                    INSERT INTO user_groups (id, [key], name, description, created_at, is_active)
                    VALUES (NEWID(), 'system-admin', 'System Administrator', 'Full access to all features', GETUTCDATE(), 1)
                END

                INSERT INTO user_group_members (user_id, group_id, created_at)
                SELECT u.id, (SELECT id FROM user_groups WHERE [key] = 'system-admin'), GETUTCDATE()
                FROM admin_users u
                WHERE u.role = 1
                AND NOT EXISTS (SELECT 1 FROM user_group_members ugm WHERE ugm.user_id = u.id AND ugm.group_id = (SELECT id FROM user_groups WHERE [key] = 'system-admin'))
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_user_group_members_users_user_id",
                table: "user_group_members",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.DropTable(
                name: "admin_users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_user_group_members_users_user_id",
                table: "user_group_members");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.CreateTable(
                name: "admin_users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    role = table.Column<int>(type: "int", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_users", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminUsers_Email",
                table: "admin_users",
                column: "email",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_user_group_members_admin_users_user_id",
                table: "user_group_members",
                column: "user_id",
                principalTable: "admin_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
