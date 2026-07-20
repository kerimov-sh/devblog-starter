using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevBlog.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPostPublishedAtIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Posts_PublishedAt",
                table: "Posts",
                column: "PublishedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Posts_PublishedAt",
                table: "Posts");
        }
    }
}
