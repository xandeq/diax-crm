using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefineAccessControl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "user_group_id",
                table: "user_group_members",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_group_members_user_group_id",
                table: "user_group_members",
                column: "user_group_id");

            migrationBuilder.AddForeignKey(
                name: "FK_user_group_members_user_groups_user_group_id",
                table: "user_group_members",
                column: "user_group_id",
                principalTable: "user_groups",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_user_group_members_user_groups_user_group_id",
                table: "user_group_members");

            migrationBuilder.DropIndex(
                name: "IX_user_group_members_user_group_id",
                table: "user_group_members");

            migrationBuilder.DropColumn(
                name: "user_group_id",
                table: "user_group_members");
        }
    }
}
