using DevBlog.Api.Data;
using DevBlog.Api.Models;
using DevBlog.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace DevBlog.Api.Endpoints;

public static class CommentsEndpoint
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/posts/{slug}/comments", async (string slug, CreateCommentRequest req, AppDbContext db) =>
        {
            var post = await db.Posts.FirstOrDefaultAsync(p => p.Slug == slug);
            if (post is null) return Results.NotFound();

            var comment = new Comment
            {
                PostId = post.Id,
                AuthorName = req.AuthorName,
                Body = req.Body,
                CreatedAt = DateTime.UtcNow
            };

            db.Comments.Add(comment);
            await db.SaveChangesAsync();

            return Results.Created($"/posts/{slug}", new { comment.Id });
        });

        app.MapGet("/comments", async (ICommentService commentService, int page = 1, int pageSize = 20) =>
        {
            var result = await commentService.GetAllCommentsAsync(page, pageSize);
            return Results.Ok(result);
        });
    }
}

public record CreateCommentRequest(string AuthorName, string Body);

public record CommentResponse(
    int Id,
    string AuthorName,
    string Body,
    DateTime CreatedAt,
    int PostId,
    string PostSlug,
    string PostTitle
);

public record PagedCommentsResponse(
    IReadOnlyList<CommentResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages
);
