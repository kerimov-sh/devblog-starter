using System.Security.Claims;
using DevBlog.Api.Services;

namespace DevBlog.Api.Endpoints;

public static class LikesEndpoint
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/posts/{slug}/like", async (string slug, ILikeService likeService, ClaimsPrincipal user) =>
        {
            var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await likeService.ToggleLikeAsync(slug, userId);

            return result.Success
                ? Results.Ok(result.Result)
                : Results.NotFound(new { error = result.Error });
        }).RequireAuthorization();
    }
}

public record LikeToggleResponse(bool Liked, int LikeCount);

public record LikeToggleResult(bool Success, LikeToggleResponse? Result, string? Error);
