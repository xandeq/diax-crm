using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DiaxDbContext))]
    [Migration("20261021140000_AddJobRuns")]
    public partial class AddJobRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "job_runs",
                columns: table => new
                {
                    job = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    run_date = table.Column<DateOnly>(type: "date", nullable: false),
                    completed_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_runs", x => new { x.job, x.run_date });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "job_runs");
        }
    }
}
