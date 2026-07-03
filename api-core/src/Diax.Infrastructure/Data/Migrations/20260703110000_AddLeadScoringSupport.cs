using System;
using Diax.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Suporte ao lead scoring com engajamento:
    /// - índice (customer_id, event_type) em email_events para agregação de opens/clicks
    /// - task_items.customer_id para vincular follow-ups a leads (dedup de tasks abertas)
    /// Migration à mão (dotnet-ef bloqueado pelo SAC) — atributos inline + snapshot manual.
    /// </summary>
    [DbContext(typeof(DiaxDbContext))]
    [Migration("20260703110000_AddLeadScoringSupport")]
    public partial class AddLeadScoringSupport : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "customer_id",
                table: "task_items",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_task_items_customer_id",
                table: "task_items",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_EmailEvent_Customer_EventType",
                table: "email_events",
                columns: new[] { "customer_id", "event_type" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_EmailEvent_Customer_EventType", table: "email_events");
            migrationBuilder.DropIndex(name: "IX_task_items_customer_id", table: "task_items");
            migrationBuilder.DropColumn(name: "customer_id", table: "task_items");
        }
    }
}
