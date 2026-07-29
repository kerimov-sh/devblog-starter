using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevBlog.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRagChunks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RagChunks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DocumentFilename = table.Column<string>(type: "TEXT", nullable: false),
                    DocumentTitle = table.Column<string>(type: "TEXT", nullable: false),
                    Heading = table.Column<string>(type: "TEXT", nullable: false),
                    ChunkIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    Embedding = table.Column<byte[]>(type: "BLOB", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RagChunks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RagChunks_DocumentFilename_ChunkIndex",
                table: "RagChunks",
                columns: new[] { "DocumentFilename", "ChunkIndex" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RagChunks");
        }
    }
}
