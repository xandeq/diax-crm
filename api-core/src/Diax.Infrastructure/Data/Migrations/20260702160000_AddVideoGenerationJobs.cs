using System;
using Diax.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Fila assíncrona de geração de vídeo (video_generation_jobs).
    /// Escrita à mão (dotnet-ef bloqueado pelo Smart App Control nesta máquina) —
    /// atributos [DbContext]/[Migration] inline substituem o Designer; o snapshot
    /// foi atualizado manualmente com o bloco correspondente.
    /// </summary>
    [DbContext(typeof(DiaxDbContext))]
    [Migration("20260702160000_AddVideoGenerationJobs")]
    public partial class AddVideoGenerationJobs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "video_generation_jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    model = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    prompt = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    negative_prompt = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    duration_seconds = table.Column<int>(type: "int", nullable: true),
                    width = table.Column<int>(type: "int", nullable: false),
                    height = table.Column<int>(type: "int", nullable: false),
                    aspect_ratio = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    seed = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    reference_image_base64 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    allow_fallback = table.Column<bool>(type: "bit", nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    provider_used = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    model_used = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    video_url = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    thumbnail_url = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    error_message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    error_category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    fallback_occurred = table.Column<bool>(type: "bit", nullable: false),
                    attempted_providers_json = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    request_id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    duration_ms = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    started_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    completed_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_video_generation_jobs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_video_generation_jobs_status_created_at",
                table: "video_generation_jobs",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_video_generation_jobs_user_id_created_at",
                table: "video_generation_jobs",
                columns: new[] { "user_id", "created_at" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "video_generation_jobs");
        }
    }
}
