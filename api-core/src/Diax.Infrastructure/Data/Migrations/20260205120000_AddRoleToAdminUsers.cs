using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleToAdminUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "role",
                table: "admin_users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Promote the initial seeded admin to Admin role (1)
            migrationBuilder.Sql(
                "UPDATE admin_users SET role = 1 WHERE id = '11111111-1111-1111-1111-111111111111';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "role",
                table: "admin_users");
        }
    }
}
