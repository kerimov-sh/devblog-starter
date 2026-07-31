using System.Security.Claims;
using DevBlog.Api.Data;
using DevBlog.Api.Models;
using DevBlog.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace DevBlog.Api.Endpoints;

public static class PostsEndpoint
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/posts", async (IPostService postService, ClaimsPrincipal user, int page = 1, int pageSize = 20, string? tag = null) =>
        {
            int? currentUserId = int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : null;
            var result = await postService.GetPostsAsync(page, pageSize, tag, currentUserId);
            return Results.Ok(result);
        });

        app.MapGet("/posts/{slug}", async (string slug, AppDbContext db, ClaimsPrincipal user) =>
        {
            int? currentUserId = int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : null;

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
                    }),
                    CommentCount = p.Comments.Count(),
                    LikeCount = db.PostLikes.Count(l => l.PostId == p.Id),
                    LikedByCurrentUser = currentUserId != null && db.PostLikes.Any(l => l.PostId == p.Id && l.UserId == currentUserId)
                })
                .FirstOrDefaultAsync();

            return post is null ? Results.NotFound() : Results.Ok(post);
        });

        app.MapPost("/posts", async (CreatePostRequest req, IPostService postService, ClaimsPrincipal user) =>
        {
            var authorId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await postService.CreatePostAsync(req, authorId);

            return result.Success
                ? Results.Created($"/posts/{result.Post!.Slug}", result.Post)
                : Results.Conflict(new { error = result.Error });
        }).RequireAuthorization();
    }
}

public record CreatePostRequest(string Title, string Content, string Slug, string Tags);

public record PostSummaryResponse(
    int Id,
    string Title,
    string Slug,
    string Tags,
    DateTime PublishedAt,
    string Author,
    int LikeCount,
    bool LikedByCurrentUser
);

public record PagedPostsResponse(
    IReadOnlyList<PostSummaryResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages
);

public record CreatePostResponse(int Id, string Slug);

public record CreatePostResult(bool Success, CreatePostResponse? Post, string? Error);
