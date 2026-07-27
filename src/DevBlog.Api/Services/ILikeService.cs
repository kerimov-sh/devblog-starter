using DevBlog.Api.Endpoints;

namespace DevBlog.Api.Services;

public interface ILikeService
{
    Task<LikeToggleResult> ToggleLikeAsync(string slug, int userId);
}
