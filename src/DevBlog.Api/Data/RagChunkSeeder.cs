using DevBlog.Api.Models;
using Microsoft.Data.Sqlite;

namespace DevBlog.Api.Data;

public static class RagChunkSeeder
{
    public static void Seed(AppDbContext db, string ragDbPath)
    {
        if (db.RagChunks.Any()) return;

        if (!File.Exists(ragDbPath))
        {
            Console.WriteLine($"[RagChunkSeeder] '{ragDbPath}' bulunamadı, RAG chat içeriksiz başlayacak.");
            return;
        }

        try
        {
            using var connection = new SqliteConnection($"Data Source={ragDbPath};Mode=ReadOnly");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT d.filename AS document_filename,
                       d.title AS document_title,
                       c.heading,
                       c.chunk_index,
                       c.content,
                       c.embedding
                FROM chunks c
                JOIN documents d ON d.id = c.document_id
                WHERE c.embedding IS NOT NULL
                ORDER BY d.order_index, c.chunk_index
                """;

            using var reader = command.ExecuteReader();
            var imported = 0;

            while (reader.Read())
            {
                db.RagChunks.Add(new RagChunk
                {
                    DocumentFilename = reader.GetString(0),
                    DocumentTitle = reader.GetString(1),
                    Heading = reader.GetString(2),
                    ChunkIndex = reader.GetInt32(3),
                    Content = reader.GetString(4),
                    Embedding = (byte[])reader.GetValue(5),
                    CreatedAt = DateTime.UtcNow
                });
                imported++;
            }

            db.SaveChanges();
            Console.WriteLine($"[RagChunkSeeder] {imported} chunk '{ragDbPath}' üzerinden import edildi.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RagChunkSeeder] '{ragDbPath}' okunurken hata oluştu, RAG chat içeriksiz başlayacak: {ex.Message}");
        }
    }
}
