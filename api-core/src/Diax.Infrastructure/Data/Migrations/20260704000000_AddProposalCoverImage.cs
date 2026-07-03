using Diax.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <summary>Capa gerada por IA na proposta pública. Migration à mão (SAC bloqueia dotnet-ef).</summary>
    [DbContext(typeof(DiaxDbContext))]
    [Migration("20260704000000_AddProposalCoverImage")]
    public partial class AddProposalCoverImage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cover_image_url",
                table: "proposals",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "cover_image_url", table: "proposals");
        }
    }
}
