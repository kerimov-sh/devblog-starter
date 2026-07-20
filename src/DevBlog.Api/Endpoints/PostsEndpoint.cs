using System.Security.Claims;
using DevBlog.Api.Data;
using DevBlog.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DevBlog.Api.Endpoints;

public static class PostsEndpoint
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/posts", async (AppDbContext db, int page = 1, int pageSize = 20) =>
        {
            page = page < 1 ? 1 : page;
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = db.Posts.AsNoTracking();

            var totalCount = await query.CountAsync();

            var posts = await query
                .OrderByDescending(p => p.PublishedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.Slug,
                    p.Tags,
                    p.PublishedAt,
                    Author = p.Author.Username
                })
                .ToListAsync();

            return Results.Ok(new
            {
                Items = posts,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        });

        app.MapGet("/posts/{slug}", async (string slug, AppDbContext db) =>
        {
            var post = await db.Posts
                .Include(p => p.Author)
                .Include(p => p.Comments)
                .Where(p => p.Slug == slug)
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.Content,
                    p.Slug,
                    p.Tags,
                    p.PublishedAt,
                    Author = p.Author.Username,
                    Comments = p.Comments.OrderBy(c => c.CreatedAt).Select(c => new
                    {
                        c.Id,
                        c.AuthorName,
                        c.Body,
                        c.CreatedAt
                    })
                })
                .FirstOrDefaultAsync();

            return post is null ? Results.NotFound() : Results.Ok(post);
        });

        // TODO: slug uniqueness validation eksik
        app.MapPost("/posts", async (CreatePostRequest req, AppDbContext db, ClaimsPrincipal user) =>
        {
            var authorId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var post = new Post
            {
                Title = req.Title,
                Content = req.Content,
                Slug = req.Slug,
                Tags = req.Tags,
                PublishedAt = DateTime.UtcNow,
                AuthorId = authorId
            };

            db.Posts.Add(post);
            await db.SaveChangesAsync();

            return Results.Created($"/posts/{post.Slug}", new { post.Id, post.Slug });
        }).RequireAuthorization();
    }
}

public record CreatePostRequest(string Title, string Content, string Slug, string Tags);
