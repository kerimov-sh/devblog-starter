using System.ComponentModel;
using ModelContextProtocol.Server;

namespace DevBlogData.Resources;

[McpServerResourceType]
public static class SchemaResource
{
    // DevBlog.Api'nin SQLite şemasının (src/DevBlog.Api/Models ve AppDbContext.OnModelCreating) elle
    // tutulan düz metin özeti. Veritabanına sorgu atmaz; şema değiştiğinde elle güncellenmelidir.
    private const string SchemaText = """
        Users
          Id: INTEGER (PK)
          Username: TEXT
          PasswordHash: TEXT
          Role: TEXT

        Posts
          Id: INTEGER (PK)
          Title: TEXT
          Content: TEXT
          Slug: TEXT (indexed)
          Tags: TEXT
          PublishedAt: TEXT (indexed, ISO-8601 datetime)
          ReadingTimeMinutes: INTEGER
          AuthorId: INTEGER (FK -> Users.Id)

        Comments
          Id: INTEGER (PK)
          PostId: INTEGER (FK -> Posts.Id)
          AuthorName: TEXT
          Body: TEXT
          CreatedAt: TEXT (ISO-8601 datetime)

        PostLikes
          Id: INTEGER (PK)
          UserId: INTEGER (FK -> Users.Id)
          PostId: INTEGER (FK -> Posts.Id)
          CreatedAt: TEXT (ISO-8601 datetime)
          Unique index: (UserId, PostId)
        """;

    [McpServerResource(UriTemplate = "devblog://schema", Name = "DevBlog Veritabanı Şeması", MimeType = "text/plain")]
    [Description("DevBlog SQLite veritabanındaki her tabloyu ve kolonlarını düz metin olarak listeler. Veritabanına sorgu göndermez; sabit bir metindir.")]
    public static string GetSchema() => SchemaText;
}
