using DevBlog.Api.Endpoints;
using DevBlog.Api.Models;
using DevBlog.Api.Repositories;

namespace DevBlog.Api.Services;

public class LikeService(ILikeRepository likeRepository, IPostRepository postRepository) : ILikeService
{
    public async Task<LikeToggleResult> ToggleLikeAsync(string slug, int userId)
    {
        var postId = await postRepository.GetIdBySlugAsync(slug);
        if (postId is null)
        {
            return new LikeToggleResult(false, null, $"Post with slug '{slug}' was not found.");
        }

        var existingLike = await likeRepository.FindAsync(userId, postId.Value);

        bool liked;
        if (existingLike is not null)
        {
            await likeRepository.RemoveAsync(existingLike);
            liked = false;
        }
        else
        {
            await likeRepository.AddAsync(new PostLike
            {
                UserId = userId,
                PostId = postId.Value,
                CreatedAt = DateTime.UtcNow
            });
            liked = true;
        }

        var likeCount = await likeRepository.CountByPostIdAsync(postId.Value);

        return new LikeToggleResult(true, new LikeToggleResponse(liked, likeCount), null);
    }
}
