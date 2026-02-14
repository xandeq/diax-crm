using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddApiKeysAndBlogPosts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "api_keys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    key_hash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    is_enabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    expires_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    last_used_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    scope = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, defaultValue: "Blog"),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_api_keys", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "blog_posts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    content_html = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    excerpt = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    meta_title = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: true),
                    meta_description = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    keywords = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    status = table.Column<int>(type: "int", nullable: false),
                    published_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    author_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    featured_image_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    view_count = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    is_featured = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    tags = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blog_posts", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_api_keys_is_enabled",
                table: "api_keys",
                column: "is_enabled");

            migrationBuilder.CreateIndex(
                name: "IX_api_keys_key_hash",
                table: "api_keys",
                column: "key_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_api_keys_name",
                table: "api_keys",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IX_blog_posts_category",
                table: "blog_posts",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "IX_blog_posts_is_featured",
                table: "blog_posts",
                column: "is_featured");

            migrationBuilder.CreateIndex(
                name: "IX_blog_posts_published_at",
                table: "blog_posts",
                column: "published_at");

            migrationBuilder.CreateIndex(
                name: "IX_blog_posts_slug",
                table: "blog_posts",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_blog_posts_status",
                table: "blog_posts",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_blog_posts_status_published_at",
                table: "blog_posts",
                columns: new[] { "status", "published_at" });

            migrationBuilder.CreateIndex(
                name: "IX_blog_posts_title",
                table: "blog_posts",
                column: "title");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "api_keys");

            migrationBuilder.DropTable(
                name: "blog_posts");
        }
    }
}
