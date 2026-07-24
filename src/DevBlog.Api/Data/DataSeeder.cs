using DevBlog.Api.Models;

namespace DevBlog.Api.Data;

public static class DataSeeder
{
    public static void Seed(AppDbContext db)
    {
        if (db.Users.Any()) return;

        var adminPasswordHash = BCrypt.Net.BCrypt.HashPassword("admin");

        var admin = new User
        {
            Username = "admin",
            PasswordHash = adminPasswordHash,
            Role = "Admin"
        };
        db.Users.Add(admin);
        db.SaveChanges();

        var posts = new[]
        {
            new Post
            {
                Title = "Getting Started with .NET Minimal APIs",
                Content = "Minimal APIs in .NET let you build lightweight HTTP APIs with minimal ceremony. " +
                          "You define endpoints directly in Program.cs or in separate extension methods, " +
                          "skipping the controller boilerplate entirely. This makes them ideal for microservices " +
                          "and small focused APIs where simplicity is key.",
                Slug = "getting-started-dotnet-minimal-apis",
                Tags = "dotnet,api,csharp",
                PublishedAt = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc),
                AuthorId = admin.Id
            },
            new Post
            {
                Title = "Entity Framework Core with SQLite",
                Content = "SQLite is a perfect companion for development and lightweight production scenarios. " +
                          "EF Core's SQLite provider requires minimal configuration — just a connection string " +
                          "pointing to a file path and the Sqlite package. Code-first migrations handle schema " +
                          "evolution cleanly.",
                Slug = "ef-core-sqlite-guide",
                Tags = "efcore,sqlite,dotnet",
                PublishedAt = new DateTime(2024, 2, 3, 9, 30, 0, DateTimeKind.Utc),
                AuthorId = admin.Id
            },
            new Post
            {
                Title = "Angular Standalone Components Explained",
                Content = "Since Angular 14, standalone components let you build Angular apps without NgModules. " +
                          "Each component declares its own imports, making the dependency graph explicit and " +
                          "tree-shaking more effective. The new application builder in Angular 17+ embraces " +
                          "standalone by default.",
                Slug = "angular-standalone-components",
                Tags = "angular,typescript,frontend",
                PublishedAt = new DateTime(2024, 3, 20, 14, 0, 0, DateTimeKind.Utc),
                AuthorId = admin.Id
            }
        };

        db.Posts.AddRange(posts);
        db.SaveChanges();

        var comments = new[]
        {
            new Comment
            {
                PostId = posts[0].Id,
                AuthorName = "Jane Dev",
                Body = "Great intro! The route handler syntax is much cleaner than the old controller approach.",
                CreatedAt = new DateTime(2024, 1, 16, 8, 0, 0, DateTimeKind.Utc)
            },
            new Comment
            {
                PostId = posts[2].Id,
                AuthorName = "Mehmet Y.",
                Body = "Finally a clear explanation of standalone components. The import list approach makes total sense.",
                CreatedAt = new DateTime(2024, 3, 21, 11, 45, 0, DateTimeKind.Utc)
            }
        };

        db.Comments.AddRange(comments);
        db.SaveChanges();
    }
}
