using System.Security.Claims;
using DevBlog.Api.Services;

namespace DevBlog.Api.Endpoints;

public static class SearchEndpoint
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/search/{arg}", async (string arg, IPostService postService, ClaimsPrincipal user, int page = 1, int pageSize = 20) =>
        {
            int? currentUserId = int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : null;
            var result = await postService.SearchPostsAsync(arg, page, pageSize, currentUserId);
            return Results.Ok(result);
        });
    }
}
