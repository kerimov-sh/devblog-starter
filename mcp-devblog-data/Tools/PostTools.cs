using System.ComponentModel;
using DevBlogData.Data;
using Microsoft.Data.Sqlite;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DevBlogData.Tools;

[McpServerToolType]
public static class PostTools
{
    [McpServerTool(Name = "get_posts")]
    [Description("Yayınlanmış blog yazılarını, yayınlanma tarihine göre en yeniden en eskiye doğru sıralayarak listeler. Her yazı için id, title, slug ve publishedat alanlarını döndürür.")]
    public static List<Dictionary<string, object?>> GetPosts(
        [Description("Döndürülecek maksimum yazı sayısı. Varsayılan değeri 10'dur.")] int limit = 10)
    {
        using var connection = OpenConnection();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Title, Slug, PublishedAt
            FROM Posts
            ORDER BY PublishedAt DESC
            LIMIT $limit
            """;
        command.Parameters.AddWithValue("$limit", limit);

        var results = new List<Dictionary<string, object?>>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new Dictionary<string, object?>
            {
                ["id"] = reader.GetInt32(0),
                ["title"] = reader.GetString(1),
                ["slug"] = reader.GetString(2),
                ["publishedat"] = reader.GetDateTime(3),
            });
        }

        return results;
    }

    [McpServerTool(Name = "get_post_by_slug")]
    [Description("Verilen slug değerine sahip blog yazısını getirir. Slug bulunamazsa exception fırlatmaz; sonucu isError=true ve açıklayıcı bir hata mesajıyla döndürür.")]
    public static CallToolResult GetPostBySlug(
        [Description("Aranacak yazının slug değeri (ör. 'getting-started-dotnet-minimal-apis').")] string slug)
    {
        using var connection = OpenConnection();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT p.Id, p.Title, p.Content, p.Slug, p.Tags, p.PublishedAt, p.ReadingTimeMinutes, u.Username
            FROM Posts p
            JOIN Users u ON u.Id = p.AuthorId
            WHERE p.Slug = $slug
            """;
        command.Parameters.AddWithValue("$slug", slug);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            // Exception fırlatmak yerine isError=true içeren bir tool sonucu döndürülüyor:
            // bu sayede JSON-RPC çağrısı protokol seviyesinde başarılı sayılır, MCP istemcisi
            // hatayı "tools/call" sonucundaki isError alanından ve content metninden okur.
            return new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = $"'{slug}' slug değerine sahip bir yazı bulunamadı." }],
            };
        }

        var post = new Dictionary<string, object?>
        {
            ["id"] = reader.GetInt32(0),
            ["title"] = reader.GetString(1),
            ["content"] = reader.GetString(2),
            ["slug"] = reader.GetString(3),
            ["tags"] = reader.GetString(4),
            ["publishedat"] = reader.GetDateTime(5),
            ["readingtimeminutes"] = reader.GetInt32(6),
            ["author"] = reader.GetString(7),
        };

        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = System.Text.Json.JsonSerializer.Serialize(post) }],
        };
    }

    private static SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection($"Data Source={DatabaseConfig.SqliteDbPath}");
        connection.Open();
        return connection;
    }
}
